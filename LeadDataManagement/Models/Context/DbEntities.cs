﻿using System;
using System.ComponentModel.DataAnnotations;

namespace LeadDataManagement.Models.Context
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public bool IsAdmin { get; set; }
        public long CreditScore { get; set; }
        public int StatusId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string ReferalCode { get; set; }
        public int? ReferedUserId { get; set; }
        public int? DiscountPercentage { get; set; }
        public string NickName { get; set; }

    }

    public class LeadType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class LeadMasterData
    {
        [Key]
        public long Id { get; set; }
        public long Phone { get; set; }
        public int LeadTypeId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

   public class UserScrub
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string LeadTypeIds { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Duration { get; set; }
        public string InputFilePath { get; set; }
        public long MatchedCount { get; set; }
        public long UnMatchedCount { get; set; }
        public string MatchedPath { get; set; }
        public string UnMatchedPath { get; set; }
        public long ScrubCredits { get; set; }
    }

    public class CreditPackage
    {
        [Key]
        public int Id { get; set; }
        public string PackageName { get; set; }
        public long Credits { get; set; }
        public long Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class UserCreditLogs
    {
        [Key]
        public long Id { get; set; }
        public string PurchaseId { get; set; }
        public int PackageId { get; set; }
        public long Credits { get; set; }
        public string TransactionDetails { get; set; }
        public int UserId { get; set; }
        public long Amount { get; set; }
        public int DiscountPercentage { get; set; }
        public Double FinalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public long ReferalUserCredits { get; set; }
    }
}