using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Group1_Expense_Tracker.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly FirestoreDb _firestoreDb;

        public ExpenseController(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<IActionResult> Details()
        {
            ViewData["ActivePage"] = "Expense";

            string userId = HttpContext.Session.GetString("FirebaseUserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Credentials");
            }

            try
            {
                DocumentReference userDocRef = _firestoreDb.Collection("Users").Document(userId);
                CollectionReference categoryCollectionRef = userDocRef.Collection("Category");
                QuerySnapshot categorySnapshot = await categoryCollectionRef.GetSnapshotAsync();

                var categories = categorySnapshot.Documents
                    .Select(doc => doc.GetValue<string>("CategoryName"))
                    .ToList();

                ViewData["Categories"] = categories;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> AddExpense(string categoryName, string title, double amount, DateTime date, string description)
        {
            string userId = HttpContext.Session.GetString("FirebaseUserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Credentials");
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                ViewData["CategoryError"] = "Please select a category.";
                return View("Details", ViewData["Categories"]);
            }

            try
            {
                DocumentReference userDocRef = _firestoreDb.Collection("Users").Document(userId);
                CollectionReference categoryCollectionRef = userDocRef.Collection("Category");

                // Log the query for categories
                Console.WriteLine("Fetching categories for userId: " + userId);

                QuerySnapshot categorySnapshot = await categoryCollectionRef.WhereEqualTo("CategoryName", categoryName).GetSnapshotAsync();
                Console.WriteLine("Category Snapshot Count: " + categorySnapshot.Documents.Count);

                DocumentReference categoryDocRef = categorySnapshot.Documents.FirstOrDefault()?.Reference;

                if (categoryDocRef != null)
                {
                    Console.WriteLine("Category found: " + categoryName);

                    CollectionReference expensesCollectionRef = categoryDocRef.Collection("Expenses");

                    QuerySnapshot expenseSnapshot = await expensesCollectionRef.WhereEqualTo("Title", title).GetSnapshotAsync();

                    if (expenseSnapshot.Documents.Any())
                    {
                        TempData["ErrorMessage"] = "Expense already exists!";
                        return RedirectToAction("Details");
                    }

                    await expensesCollectionRef.AddAsync(new
                    {
                        Title = title,
                        Amount = amount,
                        Date = Timestamp.FromDateTime(date.ToUniversalTime()),
                        Description = description
                    });

                    TempData["SuccessMessage"] = "Expense added successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    Console.WriteLine("No category found with name: " + categoryName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                Console.WriteLine("Error: " + ex.Message);
            }

            return RedirectToAction("Details");
        }

    }
}
