using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace Group1_Expense_Tracker.Models
{
    public class Category
    {
        [FirestoreProperty]
        public string UserId { get; set; }

        [FirestoreProperty]
        public List<string> CategoryNames { get; set; }

        // Optional: Add a parameterless constructor if needed
        public Category() { }
    }
}
