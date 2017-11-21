using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CourseLister.Startup))]
namespace CourseLister
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}