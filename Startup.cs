using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(GiantGroupM3.Startup))]
namespace GiantGroupM3
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
