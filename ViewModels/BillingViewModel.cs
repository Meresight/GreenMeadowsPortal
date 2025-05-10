using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GreenMeadowsPortal.ViewModels
{
    // Main ViewModel for the Billing page
    public class BillingViewModel
    {
        // User information
        public string FirstName { get; set; } = string.Empty; // Fix for CS8618
        public string LastName { get; set; } = string.Empty; // Fix for CS8618
        public string Role { get; set; } = string.Empty; // Fix for CS8618
        public string ProfileImageUrl { get; set; } = string.Empty; // Fix for CS8618
        public int NotificationCount { get; set; }

        // Current billing information
        public BillingDetailsViewModel CurrentBilling { get; set; } = new(); // Fix for CS8618

        // Statistics
        public decimal OverdueAmount { get; set; }
        public decimal TotalPaidThisYear { get; set; }
        public int DaysUntilDue { get; set; }

        // Payment methods
        public List<PaymentMethodViewModel> PaymentMethods { get; set; } = new();

        // Billing history
        public List<BillingHistoryItemViewModel> BillingHistory { get; set; } = new();

        // Filter options
        public int SelectedYear { get; set; }
        public List<SelectListItem> YearOptions { get; set; } = new();
    }

    // View model for the billing details
    public class BillingDetailsViewModel
    {
        public int BillingId { get; set; }
        public string BillingPeriod { get; set; } = string.Empty; // Fix for CS8618
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Fix for CS8618
        public List<BillingItemViewModel> BillingItems { get; set; } = new();
    }

    // View model for individual billing items
    public class BillingItemViewModel
    {
        public int ItemId { get; set; }
        public string Description { get; set; } = string.Empty; // Fix for CS8618
        public decimal Amount { get; set; }
    }

    // View model for payment methods
    public class PaymentMethodViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Fix for CS8618
        public string Details { get; set; } = string.Empty; // Fix for CS8618
        public string IconClass { get; set; } = string.Empty; // Fix for CS8618
        public bool IsDefault { get; set; }
    }

    // View model for billing history items
    public class BillingHistoryItemViewModel
    {
        public int BillingId { get; set; }
        public string BillingPeriod { get; set; } = string.Empty; // Fix for CS8618
        public decimal TotalAmount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty; // Fix for CS8618
    }
}
