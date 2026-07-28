namespace PonyUpPerformance.Web.Models
{
    public enum TitleStatus
    {
        NotProvided = -1,
        Clean = 0,
        Rebuilt = 1,
        Salvage = 2,
        Flood = 3
    }

    public enum AccidentHistory
    {
        NotProvided = -1,
        None = 0,
        Minor = 1,
        Moderate = 2,
        Major = 3
    }

    public enum MaintenanceHistory
    {
        NotProvided = -1,
        Complete = 0,
        Partial = 1,
        Unknown = 2,
        Neglected = 3
    }

    public enum RecallStatus
    {
        NotProvided = -1,
        None = 0,
        Open = 1,
        MajorSafety = 2
    }

    public enum LocalMarketAvailability
    {
        NotProvided = -1,
        VeryRare = 0,
        Rare = 1,
        Average = 2,
        Common = 3,
        VeryCommon = 4
    }

    public enum MechanicalCondition
    {
        NotProvided = -1,
        Excellent = 0,
        Good = 1,
        Fair = 2,
        Poor = 3,
        Severe = 4
    }

    public enum IntendedUse
    {
        NotProvided = -1,
        DailyDriver = 0,
        WorkVehicle = 1,
        FamilyVehicle = 2,
        ProjectVehicle = 3,
        PerformanceBuild = 4,
        RacingOrOffRoad = 5,
        Resale = 6,
        CollectorVehicle = 7
    }
}
