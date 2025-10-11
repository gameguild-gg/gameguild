using System.Collections.Concurrent;


namespace GameGuild.Modules.RatePlanExperiments
{
    public class ExperimentService : IExperimentService
    {
        private readonly ConcurrentDictionary<Guid, Experiment> _store = new();

        public Task<Experiment> CreateExperimentAsync(Experiment experiment)
        {
            _store[experiment.Id] = experiment;
            return Task.FromResult(experiment);
        }

        public Task<Experiment?> GetExperimentAsync(Guid id)
        {
            _store.TryGetValue(id, out var exp);
            return Task.FromResult(exp);
        }

        public Task<bool> ActivateVariantAsync(Guid experimentId, string variant)
        {
            if (!_store.TryGetValue(experimentId, out var exp)) return Task.FromResult(false);
            if (!exp.Variants.Contains(variant)) return Task.FromResult(false);
            // For this simple implementation, we'll mark the experiment as active and keep variants as-is
            exp.IsActive = true;
            return Task.FromResult(true);
        }
    }
}
