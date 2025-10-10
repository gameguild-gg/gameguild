using System;
using System.Threading.Tasks;

namespace GameGuild.Source.Modules.RatePlanExperiments
{
    public interface IExperimentService
    {
        Task<Experiment> CreateExperimentAsync(Experiment experiment);
        Task<Experiment?> GetExperimentAsync(Guid id);
        Task<bool> ActivateVariantAsync(Guid experimentId, string variant);
    }
}
