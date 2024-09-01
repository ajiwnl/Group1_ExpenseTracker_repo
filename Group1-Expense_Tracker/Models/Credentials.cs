using System.ComponentModel.DataAnnotations;

namespace Group1_Expense_Tracker.Models
{
	public class Credentials
	{
		[Required]
		[EmailAddress]
		public string EmailAdd { get; set;}

		[Required]
		public string Password { get; set;}

		[Required]
		public string Username { get; set; }
	}
}
