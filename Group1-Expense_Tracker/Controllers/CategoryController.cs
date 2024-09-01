using Microsoft.AspNetCore.Mvc;

namespace Group1_Expense_Tracker.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Details()
        {
            ViewData["ActivePage"] = "Category";
            return View();
        }
    }
}
