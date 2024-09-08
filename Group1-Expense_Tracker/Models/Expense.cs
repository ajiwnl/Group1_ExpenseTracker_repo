using Google.Cloud.Firestore;

namespace Group1_Expense_Tracker.Models
{

        [FirestoreData]
        public class Expense
        {
            [FirestoreProperty]
            public string Title { get; set; }

            [FirestoreProperty]
            public double Amount { get; set; }

            [FirestoreProperty]
            public Timestamp Date { get; set; }

            [FirestoreProperty]
            public string Description { get; set; }

            [FirestoreProperty]
            public string UserId { get; set; }

            [FirestoreProperty]
            public string CategoryName { get; set; }  // Add this field
        }
}
