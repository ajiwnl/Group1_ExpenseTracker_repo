using Microsoft.AspNetCore.Mvc;

namespace Group1_Expense_Tracker.Controllers
{
    public class AnalyticsController : Controller
    {
        public IActionResult Summary()
        {
            ViewData["ActivePage"] = "Analytics";
            return View();
        }


    }
}
