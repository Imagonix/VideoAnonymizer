using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Database;
using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.Web.Shared.DTO;

namespace VideoAnonymizer.ApiService.DataServices
{
    public class StateDataService(IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    {
        public async Task<AppStateDto> LoadState()
        {
            var dbContext = await dbFactory.CreateDbContextAsync();
            var modelAvailable = dbContext.SystemSettings.FirstOrDefault(x => x.Key.Equals(SystemSettingConstants.ModelAvailable));
            var appState = new AppStateDto()
            {
                ModelAvailable = modelAvailable is null ? false : modelAvailable.ReadBooleanValue(),
            };
            return appState;
        }
    }
}
