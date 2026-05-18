using UnityEngine;
using System.Collections.Generic;

//球网物理效果
public class NetImpact : MonoBehaviour
{
    [Header("施加球网的力")] public float impactForce = 300f; // 施加到球网上的力大小
    [Header("碰撞层级")] public LayerMask soccerBallLayer; // 足球对象的层级
    [Header("球网弹簧力")] public float springForce = 20f; // 弹簧力
    [Header("球网阻力")] public float damping = 6f; // 阻尼
    [Header("距离衰退")] public float distanceDecay = 1f; // 距离衰减因子
    [Header("球网向后速度")] public float initialRetreatSpeed = -10f; // 初始后退速度因子
    [Header("碰撞影响到球网范围半径")] public float influenceRadius = 5f; // 碰撞影响的半径
    [Header("球网连贯性的边界刚度")] public float boundaryStiffness = 10f; // 边界刚度

    private MeshFilter meshFilter; //网格过滤器
    private Mesh mesh; //网格
    private Vector3[] originalVertices; //原始顶点位置数组
    private Vector3[] displacedVertices; //偏移后的顶点位置数组
    private Vector3[] vertexVelocities; //顶点速度数组
    private List<Edge> edges; //网格边列表
    private HashSet<int> boundaryVertices; //边界顶点集合
    private Vector3 netNormal; //网格法线方向

    //边的结构体
    private struct Edge
    {
        public int indexA; //第一关顶点索引
        public int indexB; //第二个顶点索引
        public float restLength; //边的原始长度

        public Edge(int a, int b, float length)
        {
            indexA = a;
            indexB = b;
            restLength = length;
        }
    }

    void Start()
    {
        InitializeMesh(); //初始化网格
        CalculateNetNormal(); //计算网格的法线方向
    }

    void Update()
    {
        UpdateVertices(); //更新顶点位置
        ApplySpringForces(); //应用弹簧力
        ApplyMeshChanges(); //应用网格变更
    }

    //初始化网格数据，包含顶点、边和边界顶点
    private void InitializeMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) throw new System.Exception("MeshFilter组件缺失.");

        mesh = meshFilter.mesh;
        if (mesh == null) throw new System.Exception("Mesh未赋值.");

        originalVertices = mesh.vertices; //获取网格的原始顶点
        displacedVertices = new Vector3[originalVertices.Length]; //初始化偏移顶点数组
        vertexVelocities = new Vector3[originalVertices.Length]; //初始化顶点速度数组
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length); //复制原始顶点到偏移顶点数组

        edges = GetMeshEdges(mesh); //获取网格的边
        boundaryVertices = GetBoundaryVertices(mesh); //获取网格的边界顶点
    }

    //计算网格法线方向
    private void CalculateNetNormal()
    {
        netNormal = -transform.forward;
    }

    //更新顶点位置，应用弹簧力跟阻尼
    private void UpdateVertices()
    {
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 displacement = displacedVertices[i] - originalVertices[i]; //计算顶点的位移
            Vector3 velocity = vertexVelocities[i]; //获取顶点的当前速度
            velocity -= displacement * springForce * Time.deltaTime; //应用弹簧力
            velocity *= 1f - damping * Time.deltaTime;  //应用阻尼

            vertexVelocities[i] = velocity; //更新顶点速度
            displacedVertices[i] += velocity * Time.deltaTime; //更新定点位置

            // 约束边界顶点
            if (boundaryVertices.Contains(i))
            {
                //插值边界顶点位置
                displacedVertices[i] = Vector3.Lerp(displacedVertices[i], originalVertices[i], boundaryStiffness * Time.deltaTime);
            }
        }
    }

    //应用弹簧力到网格的每条边
    private void ApplySpringForces()
    {
        foreach (var edge in edges)
        {
            Vector3 delta = displacedVertices[edge.indexB] - displacedVertices[edge.indexA]; //计算两端顶点的位移
            float distance = delta.magnitude; //计算边的当前长度
            float forceMagnitude = springForce * (distance - edge.restLength); //计算弹簧力大小
            Vector3 force = forceMagnitude * delta.normalized; //计算力向量

            vertexVelocities[edge.indexA] += force * Time.deltaTime; //对第一关顶点应用力
            vertexVelocities[edge.indexB] -= force * Time.deltaTime; //对第二个顶点应用方向
        }
    }

    //更新网格顶点数据，并重新计算边界和法线
    private void ApplyMeshChanges()
    {
        mesh.vertices = displacedVertices; //更新网格顶点
        mesh.RecalculateBounds(); //重新计算网格边界
        mesh.RecalculateNormals(); //重新计算网格法线
    }

    private void OnCollisionEnter(Collision collision)
    {
        //检测碰撞对象是否足球
        if ((soccerBallLayer.value & (1 << collision.gameObject.layer)) == 0) return;

        Vector3 contactPoint = collision.contacts[0].point; //获取碰撞点
        Vector3 collisionVelocity = collision.relativeVelocity * impactForce; //计算碰撞速度
        collision.gameObject.GetComponent<Rigidbody>().velocity = Vector3.one; //设置足球的速度，产生一种被球网拦截的效果
        ApplyImpact(contactPoint, collisionVelocity); // 对网格应用碰撞影响
    }

    //对影响半径年的顶点应用冲击力
    private void ApplyImpact(Vector3 point, Vector3 force)
    {
        Vector3 localPoint = transform.InverseTransformPoint(point); //讲碰撞点转换为局部坐标

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 vertexWorldPosition = transform.TransformPoint(displacedVertices[i]); //讲顶点位置转换为世界坐标
            float distance = Vector3.Distance(vertexWorldPosition, point); //计算顶点与碰撞点的距离
            if (distance < influenceRadius)
            {
                //根据距离计算衰减后的力
                float attenuatedForceMagnitude = force.magnitude * Mathf.Exp(-distance * distanceDecay);
                if (attenuatedForceMagnitude < 0.01f) attenuatedForceMagnitude = 0.01f;
                Vector3 attenuatedForce = force.normalized * attenuatedForceMagnitude;

                //将力应用于网格法线方向相反的方向
                Vector3 forceDirection = -netNormal;
                vertexVelocities[i] += forceDirection * attenuatedForce.magnitude * initialRetreatSpeed * Time.deltaTime;
            }
        }
    }

    //从网格三角形生成边列表
    private List<Edge> GetMeshEdges(Mesh mesh)
    {
        var triangles = mesh.triangles; //获取网格的三角形
        var edges = new HashSet<(int, int)>(); //存储边的集合

        for (int i = 0; i < triangles.Length; i += 3)
        {
            //为每个三角形添加边
            AddEdge(edges, triangles[i], triangles[i + 1]);
            AddEdge(edges, triangles[i + 1], triangles[i + 2]);
            AddEdge(edges, triangles[i + 2], triangles[i]);
        }

        var edgeList = new List<Edge>(); //存储边的列表
        foreach (var edge in edges)
        {
            //计算每条边的原始长度
            float restLength = (originalVertices[edge.Item1] - originalVertices[edge.Item2]).magnitude;
            edgeList.Add(new Edge(edge.Item1, edge.Item2, restLength));
        }

        return edgeList;
    }

    //确保边以一致的顺序添加到集合中
    private void AddEdge(HashSet<(int, int)> edges, int a, int b)
    {
        /**
         if (a > b) (a, b) = (b, a); 
        上面写法等同下面写法，是一种元组结构赋值语法，交换变量
         if (a > b)
        {
            int temp = a;
            a = b;
            b = temp;
        }
         */

        if (a > b) (a, b) = (b, a); //确保顶点索引按顺序排列
        edges.Add((a, b)); //添加边到集合
    }

    //从网格三角形中识别边界顶点
    private HashSet<int> GetBoundaryVertices(Mesh mesh)
    {
        var boundaryVertices = new HashSet<int>(); //存储边界顶点的集合
        var triangles = mesh.triangles; //获取网格的三角形
        var edgeCount = new Dictionary<(int, int), int>(); //边的出现次数

        for (int i = 0; i < triangles.Length; i += 3)
        {
            //统计每条边的出现次数
            AddEdgeCount(edgeCount, triangles[i], triangles[i + 1]);
            AddEdgeCount(edgeCount, triangles[i + 1], triangles[i + 2]);
            AddEdgeCount(edgeCount, triangles[i + 2], triangles[i]);
        }

        //识别只出现一次的边，即是边界边
        foreach (var edge in edgeCount)
        {
            if (edge.Value == 1)
            {
                boundaryVertices.Add(edge.Key.Item1);
                boundaryVertices.Add(edge.Key.Item2);
            }
        }

        return boundaryVertices;
    }

    //统计边出现次数
    private void AddEdgeCount(Dictionary<(int, int), int> edgeCount, int a, int b)
    {
        if (a > b) (a, b) = (b, a); //确保顶点索引索引顺序排列
        if (edgeCount.ContainsKey((a, b)))
        {
            edgeCount[(a, b)]++; //增加边的计数
        }
        else
        {
            edgeCount[(a, b)] = 1; //初始化边的计数
        }
    }
}