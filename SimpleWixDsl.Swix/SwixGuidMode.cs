namespace SimpleWixDsl.Swix
{
    public enum SwixGuidMode
    {
        /// <summary>
        /// Prescribes SWIX to generate brand new guids and not read existing ones even if exists
        /// </summary>
        AlwaysGenerateNew,

        /// <summary>
        /// Tries to find existing guid.info storage files and use them; any missing guids would be generated silently;
        /// storages won't be updated.
        /// </summary>
        UseExisting,

        /// <summary>
        /// Same as UseExisting except that missing guids would be added to storage.
        /// </summary>
        UseExistingAndExtendStorage,

        /// <summary>
        /// Same as UseExistingAndExtendStorage with pruning unused guids from storage
        /// </summary>
        UseExistingAndUpdateStorage,

        /// <summary>
        /// Any requested guid that wasn't found in storages will trigger transformation error. Strict mode, basically.
        /// </summary>
        TreatAbsentGuidAsError,
    }
}