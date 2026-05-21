using UnityEngine;

public class PrintWorldPosition : MonoBehaviour
{
    private void Start()
    {
        Debug.Log($"{gameObject.name} 世界坐标 = {transform.position}");
        Debug.Log($"{gameObject.name} 本地坐标 = {transform.localPosition}");
    }
}