namespace GameGuild.Modules.RatePlanExperiments
{
    public interface IExperimentService
    {
        Task<Experiment> CreateExperimentAsync(Experiment experiment);
        Task<Experiment?> GetExperimentAsync(Guid id);
        Task<bool> ActivateVariantAsync(Guid experimentId, string variant);
    }
}
