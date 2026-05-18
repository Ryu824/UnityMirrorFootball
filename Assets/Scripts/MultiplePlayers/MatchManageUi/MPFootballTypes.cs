namespace MultiplePlayers
{
    public enum MPTeam : byte
    {
        None = 0,
        RedLeft = 1,
        BlueRight = 2
    }

    public enum MPBoundaryType : byte
    {
        None = 0,

        TouchLine_Top = 10,
        TouchLine_Bottom = 11,

        GoalLine_Left_Goal = 20,
        GoalLine_Right_Goal = 21,

        GoalLine_Left_Out_Top = 30,
        GoalLine_Left_Out_Bottom = 31,

        GoalLine_Right_Out_Top = 40,
        GoalLine_Right_Out_Bottom = 41
    }

    public enum MPRuleEventType : byte
    {
        None = 0,
        Goal = 1,
        ThrowIn = 2,
        CornerKick = 3,
        GoalKick = 4,
        PenaltyKick = 5,
        UnknownOut = 6
    }

    public enum MPMatchState : byte
    {
        Lobby = 0,
        Playing = 1,
        RulePause = 2,
        SetPiece = 3,
        TimeUp = 4,
        Closing = 5
    }


    public enum MPSetPieceMode : byte
    {
        None = 0,
        GroundKick = 1,
        ElevatedThrow = 2
    }

    public enum MPRestartLocation : byte
    {
        None = 0,
        Center = 1,

        TouchLineTop = 10,
        TouchLineBottom = 11,

        LeftTopCorner = 20,
        LeftBottomCorner = 21,
        RightTopCorner = 22,
        RightBottomCorner = 23,

        LeftGoalKick = 30,
        RightGoalKick = 31
    }

    public struct MPRuleDecision
    {
        public MPRuleEventType eventType;
        public MPTeam awardTeam;
        public MPRestartLocation restartLocation;
        public string centerMessage;

        public MPRuleDecision(
            MPRuleEventType eventType,
            MPTeam awardTeam,
            MPRestartLocation restartLocation,
            string centerMessage)
        {
            this.eventType = eventType;
            this.awardTeam = awardTeam;
            this.restartLocation = restartLocation;
            this.centerMessage = centerMessage;
        }
    }
}

namespace MultiplePlayers
{
    public enum MPTeamId : byte
    {
        None = 0,
        Red = 1,
        Blue = 2
    }

    public enum MPPlayerPosition : byte
    {
        None = 0,
        Goalkeeper = 1,
        Defender = 2,
        Midfielder = 3,
        Forward = 4
    }

    public enum MPControlType : byte
    {
        Human = 0,
        AI = 1
    }
}