using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Roles_And_Responsibility.Startup))]
namespace Roles_And_Responsibility
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
