﻿using LeadDataManagement.Helpers;
using LeadDataManagement.Models.ViewModels;
using LeadDataManagement.Services.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeadDataManagement.Controllers
{
    public class AdminController : BaseController
    {
        private IUserService userService;
        private ILeadService leadService;
        private IUserScrubService userScrubService;
        private IUserCreditLogsService userCreditLogsService;
        private ICreditPackageService creditPackageService;
        public AdminController(IUserService _userService, ILeadService _leadService, IUserScrubService userScrubService, ICreditPackageService creditPackageService, IUserCreditLogsService _userCreditLogsService)
        {
            this.userService = _userService;
            this.leadService = _leadService;
            this.userScrubService = userScrubService;
            this.creditPackageService = creditPackageService;
            this.userCreditLogsService = _userCreditLogsService;
        }

        #region Dashboard
        public ActionResult Dashboard()
        {
            if(!this.CurrentLoggedInUser.IsAdmin)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            ViewBag.PstTime = DateTimeHelper.GetDateTimeNowByTimeZone(DateTimeHelper.TimeZoneList.PacificStandardTime).ToString("yyyy-MM-dd");

            return View();
        }
        public ActionResult UserScrubsGrid(DateTime date)
        {

            var usersList = userService.GetUsers().ToList();
            var Leads = leadService.GetLeadTypes().ToList();
            List<UserScrubsGridModel> retData = new List<UserScrubsGridModel>();
            var userScrubs = userScrubService.GetAllUserScrubs().OrderByDescending(x=>x.CreatedDate).ToList().Where(x=>x.CreatedDate.Date==date.Date);
            int iCount = 0;
            foreach (var u in userScrubs)
            {
                iCount += 1;
                List<int> leadTypes = JsonConvert.DeserializeObject<List<DropDownModel>>(u.LeadTypeIds).Select(x => x.Id).ToList();
                string InputExtensions = u.InputFilePath.Split('.')[1];
                retData.Add(new UserScrubsGridModel()
                {
                    Sno = iCount,
                    UserName= usersList.Where(x=>x.Id==u.UserId).FirstOrDefault().Name,
                    ScrubCredits = LeadsHelpers.ToUsNumberFormat(u.ScrubCredits),
                    CreatedAt = u.CreatedDate.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    LeadType = String.Join(",", Leads.Where(x => leadTypes.Contains(x.Id)).Select(x => x.Name).ToList()),
                    Matched = "Input File  <a href='" + u.InputFilePath + "' style='cursor:pointer' download='InputFile-" + u.Id + "." + InputExtensions + "'><i class='fa fa-download' ></i></a><br>"+"Matched- " + u.MatchedCount + " <a href='" + u.MatchedPath + ".csv' style='cursor:pointer' download='Matched-" + u.Id + ".csv'><i class='fa fa-download' ></i></a><br>"+ "Clean- " + u.UnMatchedCount + " <a href='" + u.UnMatchedPath + ".csv' style='cursor:pointer' download='UnMatched-" + u.Id + ".csv'><i class='fa fa-download' ></i></a>",
                });
            }
            var jsonData = new { data = from emp in retData select emp };
            return new JsonResult()
            {
                Data = jsonData,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = Int32.MaxValue
            };
        }
        public ActionResult UserCreditLogsGrid(DateTime date)
        {
            List<UserCreditLogGridViewModel> retData = new List<UserCreditLogGridViewModel>();
            var userCreditLogs = userCreditLogsService.GetAllUserCreditLogs().ToList().Where(x => x.CreatedAt.Date==date.Date);
            var users = userService.GetUsers().ToList();
            int iCount = 0;
            foreach (var u in userCreditLogs)
            {
                var thisUser = users.Where(x => x.Id == u.UserId).FirstOrDefault();
                string ReferalDetails = string.Empty;
                if(thisUser.ReferedUserId.HasValue && thisUser.ReferedUserId.Value>0)
                {
                    ReferalDetails = string.Format("Referral Bonus {0} to {1}",LeadsHelpers.ToUsNumberFormat(u.ReferalUserCredits), users.Where(x => x.Id == thisUser.ReferedUserId.Value).FirstOrDefault().Name);
                }
                iCount += 1;
                retData.Add(new UserCreditLogGridViewModel()
                {
                    SNo = iCount,
                    Id = u.Id,
                    UserName= thisUser.Name,
                    Date = u.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    CreatedAt = u.CreatedAt,
                    Credits = LeadsHelpers.ToUsNumberFormat(u.Credits),
                    DisCountPercentage = u.DiscountPercentage.ToString(),
                    AmountPaid = Math.Round(u.FinalAmount, 2).ToString(),
                    PackageName = creditPackageService.GetAllCreditPackages().FirstOrDefault(x => x.Id == u.PackageId).PackageName,
                    ReferalInfo= ReferalDetails
                });
            }
            retData = retData.OrderByDescending(x => x.CreatedAt).ToList();
            var jsonData = new { data = from emp in retData select emp };
            return new JsonResult()
            {
                Data = jsonData,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = Int32.MaxValue
            };
        }

        #endregion

        #region Users
        public ActionResult Users()
        {
            if (!this.CurrentLoggedInUser.IsAdmin)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            return View();
        }
        public ActionResult UsersGrid()
        {
            var userScrubLogs = userScrubService.GetAllUserScrubs().ToList();
            List<UserGridViewModel> retData = new List<UserGridViewModel>();
            retData = userService.GetUsers().Where(x => x.IsAdmin == false).ToList().Select(x => new UserGridViewModel 
            { 
                Id=x.Id,
                Name=x.Name,
                Email=x.Email,
                NickName=x.NickName,
                Password=x.Password,
                CreatedAt=x.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                CreditScore=x.CreditScore,
                Status = userService.GetStatusById(x.StatusId),
                ModifiedAt=x.ModifiedAt.HasValue?x.ModifiedAt.Value.ToString("dd-MMM-yyyy hh:mm:ss tt"):string.Empty,
                StatusId=x.StatusId,
                ReferalCode=x.ReferalCode,
                ReferedById = x.ReferedUserId.HasValue ? x.ReferedUserId.Value : 0,
                DiscountPercentage=x.DiscountPercentage.HasValue?x.DiscountPercentage.Value:0
            }).ToList();
            int iCount = 0;
            foreach(var r in retData)
            {
                iCount += 1;
                r.SNo = iCount;
                r.CreditScoreStr = LeadsHelpers.ToUsNumberFormat(r.CreditScore-userScrubLogs.Where(x=>x.UserId==r.Id).Sum(x => x.ScrubCredits));
                if (r.StatusId==1)
                {
                    r.EditBtn = "<button type='button' class='btn btn-success m-b-10 btnapprove btn-sm' data-id='" + r.Id+ "'data-discountPercentage='"+ r.DiscountPercentage +"' data-score='" + r.CreditScore+ "' data-nick='"+ r.NickName+"'>Approve</button>";
                }else if(r.StatusId==2)
                {
                    r.EditBtn = "<button type='button' class='btn btn-danger m-b-10 btninactivate btn-sm' data-id='" + r.Id + "' data-discountPercentage='"+ r.DiscountPercentage +"' data-score='" + r.CreditScore + "' data-nick='" + r.NickName + "'>In-Activate</button> &nbsp;&nbsp;" + 
                        "<button type = 'button' class='btn btn-success m-b-10 btnedit btn-sm' data-id='" + r.Id+ "' data-discountPercentage='" + r.DiscountPercentage + "' data-status='" + r.StatusId+"' data-score='"+r.CreditScore + "' data-nick='" + r.NickName + "'>Edit</button>";
                }
                else
                {
                    r.EditBtn = "<button type='button' class='btn btn-primary m-b-10 btnactivate btn-sm' data-id='" + r.Id + "' data-discountPercentage='" + r.DiscountPercentage + "' data-score='" + r.CreditScore + "' data-nick='" + r.NickName + "'>Activate</button>&nbsp;&nbsp;"
                        + "<button type = 'button' class='btn btn-success m-b-10 btnedit btn-sm' data-id='" + r.Id + "' data-discountPercentage='" + r.DiscountPercentage + "' data-status='" + r.StatusId + "' data-score='" + r.CreditScore + "' data-nick='" + r.NickName + "'>Edit</button>";
                }
                if(r.ReferedById!=0)
                {
                    r.RefedByUserName = userService.GetUsers().FirstOrDefault(x => x.Id == r.ReferedById).Name;
                }
            }
            var jsonData = new { data = from emp in retData select emp };
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateUserStatus(int userId,long creditScore,int statusId,int discountPercentage,string nickName)
        {
            try
            {
                userService.UpdateUserStatus(userId, creditScore, statusId, discountPercentage,nickName);
                
            }catch(Exception ex)
            {
                throw ex;
            }
            return Json("",JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region Lead Types

        public ActionResult LeadTypes()
        {
            if (!this.CurrentLoggedInUser.IsAdmin)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            return View();
        }
        public ActionResult LeadTypeGrid()
        {
            List<LeadTypeGridViewModel> ret = new List<LeadTypeGridViewModel>();
            var leadTypesList = leadService.GetLeadTypes().OrderBy(x => x.Name).ToList();
            int iCount = 0;
            foreach (var l in leadTypesList)
            {
                iCount += 1;
                LeadTypeGridViewModel retData = new LeadTypeGridViewModel();
                retData.LeadType = l.Name;
                retData.SNo = iCount;
                retData.Status = l.IsActive == true ? "Active" : "InActive";
                retData.Id = l.Id;
                retData.CreatedAt = l.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt");
                retData.IsActive = l.IsActive;
                retData.EditBtn = "<button type='button' class='btn btn-primary m-b-10 btnedit btn-sm' data-lead='" + l.Name + "' data-id='" + l.Id + "'><i class='fa fa-pencil-square-o'></i> Edit</button>";
                if(l.IsActive)
                {
                    retData.EditBtn+= "&nbsp;&nbsp;<button type='button' class='btn btn-danger m-b-10 btnenabledisable btn-sm' data-id='" + l.Id + "'>Disable</button>";
                }
                else
                {
                    retData.EditBtn += "&nbsp;&nbsp;<button type='button' class='btn btn-success m-b-10 btnenabledisable btn-sm' data-id='" + l.Id + "'>Enable</button>";
                }
                ret.Add(retData);
            }
            var jsonData = new { data = from emp in ret select emp };
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }
        public ActionResult UpdateLeadTypeStatus(int id)
        {
            leadService.UpdateLeadTypeStatus(id);
            return Json("", JsonRequestBehavior.AllowGet); 
        }
        public ActionResult AddEditLeadType(int id,string leadType)
        {
            if(leadService.GetLeadTypes().Any(x=>x.Name.ToLower()==leadType.ToLower() && x.Id!=id))
            {
                return Json("Duplicate lead type not allowed", JsonRequestBehavior.AllowGet);
            }
            leadService.AddEditLeadTypes(id, leadType);
            return Json("", JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Lead Data
        public ActionResult LeadMasterData()
        {
            if (!this.CurrentLoggedInUser.IsAdmin)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            ViewBag.LeadTypesList = leadService.GetLeadTypes().Where(x=>x.IsActive).ToList().Select(x => new DropDownModel()
            {
                Name=x.Name,
                Id=x.Id
            }).OrderBy(x=>x.Name).ToList();
            return View();
        }
        public ActionResult LeadMasterDataGrid(int? leadTypeId)
        {
            var leadTypesList = leadService.GetLeadTypes().ToList();
            List<LeadMasterDataGridViewModel> retData = new List<LeadMasterDataGridViewModel>();
            var leadList = leadService.GetLeadMasterdataGridList(leadTypeId);
            int iCount = 0;
            foreach (var l in leadList)
            {
                iCount += 1;
                retData.Add(new LeadMasterDataGridViewModel()
                {
                    SNo = iCount,
                    Id = l.Id,
                    PhoneCount = l.Count,
                    LeadType=l.Name,
                    EditBtn = "<button type='button' class='btn btn-primary m-b-10 btnView btn-sm' data-name='"+l.Name+"' data-id='" + l.Id + "' ><i class='fa fa-eye'></i> View Data</button>"
                }); ;
            }
            var jsonData = new { data = from emp in retData select emp };
            return new JsonResult()
            {
                Data = jsonData,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = Int32.MaxValue // Use this value to set your maximum size for all of your Requests
            };
        }
        public JsonResult GetViewList(int leadTypeId)
        {
            var data = leadService.GetAllLeadMasterDataByLeadType(leadTypeId).Select(x=>x.Phone).ToArray();
            string retData = string.Join(", ", data);
            return new JsonResult()
            {
                Data = retData,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = Int32.MaxValue 
            };
        }

        public ActionResult UploadMasterData(FormCollection formCollection,int LeadTypeId)
        {
            if (Request != null)
            {
                HttpPostedFileBase file = Request.Files["MasterLoadFile"];
                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string path = Server.MapPath("~/Content/DataLoads/" + file.FileName);
                    file.SaveAs(path);
                    
                    string[] lines = System.IO.File.ReadAllLines(path);
                    List<long> PhoneNo = lines.Select(x => Convert.ToInt64(x.Replace(",","").Trim())).Take(300000).ToList();
                    leadService.SaveMasterData(PhoneNo, LeadTypeId);
                }
            }
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            ViewBag.LeadTypesList = leadService.GetLeadTypes().Where(x => x.IsActive).ToList().Select(x => new DropDownModel()
            {
                Name = x.Name,
                Id = x.Id
            }).OrderBy(x => x.Name).ToList();
            return View("LeadMasterData");
        }
        #endregion

        #region Packages
        public ActionResult Packages()
        {
            if (!this.CurrentLoggedInUser.IsAdmin)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            return View();
        }
        public ActionResult CreditPackageGrid()
        {
            List<CreditPackageViewModel> retData = new List<CreditPackageViewModel>();
            var leadTypesList = creditPackageService.GetAllCreditPackages().OrderBy(x => x.PackageName).ToList();
            int iCount = 0;
            foreach (var l in leadTypesList)
            {
                iCount += 1;
                retData.Add(new CreditPackageViewModel()
                {
                    Id = l.Id,
                    SNo = iCount,
                    Status = l.IsActive == true ? "Active" : "InActive",
                    CreatedDate = l.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    IsActive = l.IsActive,
                    PackageName=l.PackageName,
                    Credits=l.Credits,
                    CreditsStr=LeadsHelpers.ToUsNumberFormat(l.Credits),
                    Price=l.Price,
                    EditBtn = "<button type='button' class='btn btn-primary m-b-10 btnedit btn-sm' data-id='" + l.Id + "' data-packagename='" + l.PackageName + "' data-credits='" + l.Credits + "' data-price='" + l.Price + "' data-status='" + l.IsActive + "' data-id='" + l.Id + "'><i class='fa fa-pencil-square-o'></i> Edit</button>"
                });
            }
            var jsonData = new { data = from emp in retData select emp };
            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddUpdatePackage(int id,string packageName,long credits,long price, bool status)
        {
            bool isExists = creditPackageService.GetAllCreditPackages().Any(x => x.Id != id && x.PackageName.ToLower() == packageName.ToLower());
            if(isExists)
            {
                return Json("Duplicate package not allowed", JsonRequestBehavior.AllowGet);
            }
            creditPackageService.SavePackage(id, packageName, credits, price, status);

            return Json("", JsonRequestBehavior.AllowGet);
        }
        #endregion



    }
}