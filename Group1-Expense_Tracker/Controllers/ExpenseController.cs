using Microsoft.AspNetCore.Mvc;

namespace Group1_Expense_Tracker.Controllers
{
    public class ExpenseController : Controller
    {
        public IActionResult Details()
        {
            ViewData["ActivePage"] = "Expense";
            return View();
        }
    }
}
