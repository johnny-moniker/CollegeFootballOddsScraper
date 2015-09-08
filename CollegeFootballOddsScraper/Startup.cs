using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CollegeFootballOddsScraper.Startup))]
namespace CollegeFootballOddsScraper
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
