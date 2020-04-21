namespace VibesSwap.Model.Dimensional
{
    internal enum HostTypes
    {
        COMM1,
        COMM2,
        EXEC,
        OPERDB,
        OPERAPP1,
        OPERAPP2,
        MS,
        BPI,
        ENS
    }

    internal enum CmTypes
    {
        CM_EC,
        CM_LM,
        CM_PDM,
        CM_TFC,
        CM_OPM,
        CM_ODB,
        CM_BPI,
        CM_MS,
        CM_ENS,
        CM_MDS,
        CM_PSS,
        CM_BSIS
    }

    internal enum CmStates
    {
        Alive,
        Offline,
        Unchecked,
        Polling
    }
}
