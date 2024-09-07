using Firebase.Auth;
using Google.Cloud.Firestore;

namespace Group1_Expense_Tracker
{
        public class Startup
        {
            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            public void ConfigureServices(IServiceCollection services)
            {

            // Set the environment variable for Firestore credentials
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "group1-expensetracker-firebase-adminsdk-yqaqz-b62c13a049.json");
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            // Add FirebaseAuthProvider and FirestoreDb as services
            services.AddSingleton(new FirebaseAuthProvider(new FirebaseConfig("AIzaSyAhHT-TnETQg_ow8H_50R5p2c69_ZLVLMU")));
            services.AddSingleton(FirestoreDb.Create("group1-expensetracker"));
            services.AddSession();
 ;
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                app.UseSession();
            }
        }
}
