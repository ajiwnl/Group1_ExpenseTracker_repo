using Microsoft.AspNetCore.Mvc;

namespace Group1_Expense_Tracker.Controllers
{
    public class PrefController : Controller
    {
        public IActionResult Settings()
        {
            ViewData["ActivePage"] = "Settings";
            return View();
        }
    }
}
