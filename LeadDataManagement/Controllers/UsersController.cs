﻿using ExcelDataReader;
using LeadDataManagement.Helpers;
using LeadDataManagement.Models.ViewModels;
using LeadDataManagement.Services.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace LeadDataManagement.Controllers
{
    public class UsersController : BaseController
    {
        private IUserScrubService userScrubService;
        private IUserService userService;
        private ILeadService leadService;
        private IUserCreditLogsService userCreditLogsService;
        private ICreditPackageService creditPackageService;
        private string filterCols = "Phone,Ph,Home Phone,Telephone,Phone Home,phone home,PhoneHome,phonehome,phone,home phone,telephone";
        public UsersController(IUserScrubService _userScrubService, IUserService _userService, ILeadService _leadService, IUserCreditLogsService _userCreditLogsService, ICreditPackageService _creditPackageService)
        {
            userScrubService = _userScrubService;
            userService = _userService;
            leadService = _leadService;
            userCreditLogsService = _userCreditLogsService;
            creditPackageService = _creditPackageService;
        }
        public ActionResult Dashboard()
        {
            ViewBag.PayPalClientId = "AdpPKo_1ekKlG7W9Njp8INCxwYKhDACND1RMgZZzliXv0YjhFLCnZ507Jlu0F7LBPNHNIVmFHe4njTKl";
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            List<DropDownModel> packageList = new List<DropDownModel>();
            var packagesList = creditPackageService.GetAllCreditPackages().Where(x => x.IsActive == true).ToList().Select(x => new DropDownModel
            {
                Id = x.Id,
                Name = x.PackageName,
                Count = x.Credits,
                IsUnlimited = x.IsUnlimitedPackage.HasValue?x.IsUnlimitedPackage.Value:false,
                Amount = x.IsUnlimitedPackage.HasValue && x.IsUnlimitedPackage.Value==false? x.Price: CalculateUnlimitedPackagePrice(x.Price)
            }).ToList();
            ViewBag.PackagesList = packagesList;
            return View();
        }
        private float CalculateUnlimitedPackagePrice(long originalPrice)
        {
            float retVal = 0f;
            var todayDate = DateTimeHelper.GetDateTimeNowByTimeZone(DateTimeHelper.TimeZoneList.PacificStandardTime);
            int daysInMonth= DateTime.DaysInMonth(todayDate.Year, todayDate.Month);
            DateTime monthLastDate = new DateTime(todayDate.Year, todayDate.Month, daysInMonth);
            int totalSignUpDays =(int)(monthLastDate - todayDate).TotalDays+1;
            decimal perdayVal = Math.Round(((decimal)totalSignUpDays / (decimal)daysInMonth), 2);
            retVal = (float)(perdayVal * originalPrice);
            return retVal;
        }
        public JsonResult GetUserCreditsDetails()
        {
            var usedCredits = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x=>x.IsUnlimitedPackageInActivation==false).Sum(x => x.ScrubCredits);
            var userTotalCredits = this.CurrentLoggedInUser.CreditScore;
            var remainingCredits = userTotalCredits - usedCredits;
            return Json(new
            {
                remainingCredits =LeadsHelpers.ToUsNumberFormat(remainingCredits),
                totalCredits = LeadsHelpers.ToUsNumberFormat(userTotalCredits),
            }, JsonRequestBehavior.AllowGet);
        }
        #region User Scrub

        public ActionResult Scrubber()
        {
            ViewBag.IsFileError = "false";
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            ViewBag.LeadTypesList = leadService.GetLeadTypes().Where(x=>x.IsActive==true).ToList().Select(x => new DropDownModel()
            {
                Name = x.Name,
                Id = x.Id
            }).OrderBy(x => x.Name).ToList();
            var usedCredits = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x => x.IsUnlimitedPackageInActivation == false).Sum(x => x.ScrubCredits);
            var userTotalCredits = this.CurrentLoggedInUser.CreditScore;
            ViewBag.remainingCredits =LeadsHelpers.ToUsNumberFormat(userTotalCredits - usedCredits);
            ViewBag.totalCredits = LeadsHelpers.ToUsNumberFormat(userTotalCredits);
            ViewBag.NoCredits = false;
            return View();
        }

        public ActionResult UserScrubsGrid()
        {
            var nowDate = DateTimeHelper.GetDateTimeNowByTimeZone(DateTimeHelper.TimeZoneList.PacificStandardTime);
            var monthStartDate= new DateTime(nowDate.Year, nowDate.Month,1);
            var Leads = leadService.GetLeadTypes().ToList();
            List<UserScrubsGridModel> retData = new List<UserScrubsGridModel>();
            var userScrubs = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x => x.CreatedDate.Date >= monthStartDate.Date && x.CreatedDate.Date <= nowDate.Date);
            int iCount = 0;
            foreach (var u in userScrubs)
            {
                iCount += 1;
                List<int> leadTypes = JsonConvert.DeserializeObject<List<DropDownModel>>(u.LeadTypeIds).Select(x => x.Id).ToList();
                string InputExtensions = u.InputFilePath.Split('.')[1];
                retData.Add(new UserScrubsGridModel()
                {
                    Sno = iCount,
                    ScrubCredits =LeadsHelpers.ToUsNumberFormat(u.ScrubCredits),
                    CreatedAt = u.CreatedDate.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    LeadType = String.Join(",", Leads.Where(x => leadTypes.Contains(x.Id)).Select(x => x.Name).ToList()),
                    Matched = "Matched- " + u.MatchedCount + " <a href='" + u.MatchedPath + ".csv' style='cursor:pointer' download='Matched-" + u.Id + ".csv'><i class='fa fa-download' ></i></a><br>" + "Clean- " + u.UnMatchedCount + " <a href='" + u.UnMatchedPath + ".csv' style='cursor:pointer' download='UnMatched-" + u.Id + ".csv'><i class='fa fa-download' ></i></a>",
                    Duration = u.Duration,
                    InputFile = "Input File  <a href='" + u.InputFilePath + "' style='cursor:pointer' download='InputFile-" + u.Id + "." + InputExtensions + "'><i class='fa fa-download' ></i></a>",
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
        private bool checkUnlimitedPackageInActivation()
        {
            bool retVal = false;
            var dateNow = DateTimeHelper.GetDateTimeNowByTimeZone(DateTimeHelper.TimeZoneList.PacificStandardTime);
            var userPackagesList = userCreditLogsService.GetAllUserCreditLogs().Where(x => x.UserId == this.CurrentLoggedInUser.Id && x.CreatedAt.Month == dateNow.Month).Select(x => x.PackageId).Distinct().ToList();
            retVal=creditPackageService.GetAllCreditPackages().Any(x => userPackagesList.Contains(x.Id) && x.IsUnlimitedPackage == true);
            return retVal;
        }
        public ActionResult PerformUserScrub(FormCollection formCollection, string PhoneNos, string SelectedLeads)
        {
            bool isError = false;
            try
            {
                bool isUnlimitedPackageInActivation = checkUnlimitedPackageInActivation();
                var used = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x => x.IsUnlimitedPackageInActivation == false).Sum(x => x.ScrubCredits);
                Stopwatch sw = Stopwatch.StartNew();
                List<int> selectedLeads = JsonConvert.DeserializeObject<List<DropDownModel>>(SelectedLeads).Select(x => x.Id).ToList();
                if (string.IsNullOrEmpty(PhoneNos))
                {
                    HttpPostedFileBase file = Request.Files["ScrubFile"];
                    if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    {
                        string ext = Path.GetExtension(file.FileName);
                        string newFileName = Guid.NewGuid().ToString();
                        string path = Path.Combine(Server.MapPath("~/Content/DataLoads/"), newFileName + ext);
                        file.SaveAs(path);

                        List<long> UserScrubPhonesList = new List<long>();
                        DataTable excelDt = new DataTable();
                        string phoneCol = string.Empty;
                        List<DropDownModel> dt = new List<DropDownModel>();
                        if (ext.ToLower() == ".csv")
                        {
                            excelDt = ReadCsvFile(path, ref phoneCol);
                            dt = FilterDatatableColoumn(excelDt.AsEnumerable(), excelDt, phoneCol);
                        }
                        else
                        {
                            excelDt = ReadExcelFile(ext, path,ref phoneCol);
                            dt = FilterDatatableColoumn(excelDt.AsEnumerable(), excelDt, phoneCol);
                        }
                        foreach (var r in dt)
                        {
                            long number;
                            bool isSuccess = Int64.TryParse(r.Name, out number);
                            if (isSuccess)
                            {
                                UserScrubPhonesList.Add(number);
                            }
                        }
                        if (UserScrubPhonesList.Count > 0)
                        {

                            if (isUnlimitedPackageInActivation || ((this.CurrentLoggedInUser.CreditScore - used) >= UserScrubPhonesList.Count()))
                            {
                                var matchedList = leadService.ScrubPhoneNos(selectedLeads, UserScrubPhonesList).ToList();
                                var unmatchedCount = UserScrubPhonesList.Except(matchedList).ToList();
                                var dtList = excelDt.AsEnumerable();
                                var matchedDt = GetFilteredPhones(dtList, matchedList, phoneCol);
                                var unMatchedDt = GetFilteredPhones(dtList, unmatchedCount, phoneCol);
                                sw.Stop();

                                //  Matched File Create
                                string matchedFileName = Guid.NewGuid().ToString();
                                CreatedDataTableCSV(matchedDt, matchedFileName);

                                // UnMatched File Create
                                string unMatchedFileName = Guid.NewGuid().ToString();
                                CreatedDataTableCSV(unMatchedDt, unMatchedFileName);

                                userScrubService.SaveUserScrub(UserScrubPhonesList.Count(), this.CurrentLoggedInUser.Id, SelectedLeads, matchedList.Count(), unmatchedCount.Count(), matchedFileName, unMatchedFileName, newFileName + ext, sw.Elapsed.Seconds,isUnlimitedPackageInActivation);
                            }
                            else
                            {
                                ViewBag.NoCredits = true;
                                ViewBag.IsFileError = false;
                                ViewBag.CurrentUser = this.CurrentLoggedInUser;
                                ViewBag.LeadTypesList = leadService.GetLeadTypes().Where(x=>x.IsActive==true).ToList().Select(x => new DropDownModel()
                                {
                                    Name = x.Name,
                                    Id = x.Id
                                }).OrderBy(x => x.Name).ToList();
                                var usedCredits1 = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x => x.IsUnlimitedPackageInActivation == false).Sum(x => x.ScrubCredits);
                                var userTotalCredits1 = this.CurrentLoggedInUser.CreditScore;
                                ViewBag.remainingCredits = LeadsHelpers.ToUsNumberFormat(userTotalCredits1 - usedCredits1);
                                ViewBag.totalCredits = LeadsHelpers.ToUsNumberFormat(userTotalCredits1);
                                return View("Scrubber");
                            }
                        }
                    }
                }
                else
                {
                    List<string> inputList = PhoneNos.Split(',').ToList();
                    List<long> UserScrubPhonesList = new List<long>();
                    foreach (var i in inputList)
                    {
                        long number;
                        bool isSuccess = Int64.TryParse(LeadsHelpers.ProcessNumber(i), out number);
                        if (isSuccess)
                        {
                            UserScrubPhonesList.Add(number);
                        }
                    }
                    if (UserScrubPhonesList.Count > 0)
                    {
                        if (isUnlimitedPackageInActivation || ((this.CurrentLoggedInUser.CreditScore - used) >= UserScrubPhonesList.Count()))
                        {
                            var matchedList = leadService.ScrubPhoneNos(selectedLeads, UserScrubPhonesList).ToList();
                            var unmatchedCount = UserScrubPhonesList.Except(matchedList).ToList();

                            sw.Stop();

                            //InputFile Create
                            string inputFileName = Guid.NewGuid().ToString();
                            CreateSaveCsvFile(inputFileName, UserScrubPhonesList);

                            //Matched File Create
                            string matchedFileName = Guid.NewGuid().ToString();
                            CreateSaveCsvFile(matchedFileName, matchedList);

                            // UnMatched File Create
                            string unMatchedFileName = Guid.NewGuid().ToString();
                            CreateSaveCsvFile(unMatchedFileName, unmatchedCount);

                            userScrubService.SaveUserScrub(UserScrubPhonesList.Count(), this.CurrentLoggedInUser.Id, SelectedLeads, matchedList.Count(), unmatchedCount.Count(), matchedFileName, unMatchedFileName, inputFileName + ".csv", sw.Elapsed.Seconds, isUnlimitedPackageInActivation);
                        }
                        else
                        {
                            ViewBag.NoCredits = true;
                            ViewBag.IsFileError = false;
                            ViewBag.CurrentUser = this.CurrentLoggedInUser;
                            ViewBag.LeadTypesList = leadService.GetLeadTypes().Where(x=>x.IsActive==true).ToList().Select(x => new DropDownModel()
                            {
                                Name = x.Name,
                                Id = x.Id
                            }).OrderBy(x => x.Name).ToList();
                            var usedCredits1 = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x => x.IsUnlimitedPackageInActivation == false).Sum(x => x.ScrubCredits);
                            var userTotalCredits1 = this.CurrentLoggedInUser.CreditScore;
                            ViewBag.remainingCredits =LeadsHelpers.ToUsNumberFormat(userTotalCredits1 - usedCredits1);
                            ViewBag.totalCredits = LeadsHelpers.ToUsNumberFormat(userTotalCredits1);
                            return View("Scrubber");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                isError = true;
            }
            ViewBag.NoCredits = false;
            ViewBag.IsFileError = isError;
            ViewBag.CurrentUser = this.CurrentLoggedInUser;
            ViewBag.LeadTypesList = leadService.GetLeadTypes().Where(x=>x.IsActive==true).ToList().Select(x => new DropDownModel()
            {
                Name = x.Name,
                Id = x.Id
            }).OrderBy(x => x.Name).ToList();
            var usedCredits = userScrubService.GetScrubsByUserId(this.CurrentLoggedInUser.Id).Where(x => x.IsUnlimitedPackageInActivation == false).Sum(x => x.ScrubCredits);
            var userTotalCredits = this.CurrentLoggedInUser.CreditScore;
            ViewBag.remainingCredits =LeadsHelpers.ToUsNumberFormat(userTotalCredits - usedCredits);
            ViewBag.totalCredits =LeadsHelpers.ToUsNumberFormat(userTotalCredits);
            return View("Scrubber");
        }


        #region User Scrub Private Functions
        private DataTable GetFilteredPhones(EnumerableRowCollection<DataRow> uplodedFileDtRows, List<long> phonesNos, string phoneColName)
        {
            List<string> searchPhonesList = phonesNos.Select(x => x.ToString()).ToList();
            var newretDt = new DataTable();
            if (phonesNos.Count > 0)
                newretDt = uplodedFileDtRows.Where(x => searchPhonesList.Contains(x.Field<string>(phoneColName))).CopyToDataTable();
            return newretDt;
        }
       
        private List<DropDownModel> FilterDatatableColoumn(EnumerableRowCollection<DataRow> uplodedFileDtRows, DataTable RecordDT_, string col)
        {
            try
            {
                var dtt = uplodedFileDtRows.Select(x => new DropDownModel
                {
                    Name = x.Field<string>(col)
                }).ToList();
                //DataTable TempTable = RecordDT_;
                //DataView view = new DataView(TempTable);
                //DataTable selected = view.ToTable("Selected", false, col);
                return dtt;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private DataTable ReadExcelFile(string ext, string path, ref string phoneCol)
        {
            DataTable excelDt = new DataTable();
            FileStream stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read);
            IExcelDataReader excelReader = null;
            if (ext == ".xls")
                excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            else
                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

            int fieldcount = excelReader.FieldCount;
            int rowcount = excelReader.RowCount;

            DataRow row;
            DataTable dt_ = new DataTable();
            dt_ = excelReader.AsDataSet().Tables[0];
            for (int i = 0; i < dt_.Columns.Count; i++)
            {
                var thisCol = dt_.Rows[0][i].ToString();
                excelDt.Columns.Add(thisCol);
                if (filterCols.Split(',').ToList().Contains(thisCol))
                {
                    phoneCol = thisCol;
                }
            }
            int rowcounter = 0;
            for (int row_ = 1; row_ < dt_.Rows.Count; row_++)
            {
                row = excelDt.NewRow();
                for (int col = 0; col < dt_.Columns.Count; col++)
                {
                    row[col] = LeadsHelpers.ProcessNumber(dt_.Rows[row_][col].ToString());
                    rowcounter++;
                }
                excelDt.Rows.Add(row);
            }
            excelReader.Close();
            excelReader.Dispose();
            return excelDt;
        }
        
        private DataTable ReadCsvFile(string path, ref string phoneCol)
        {
            string empty = string.Empty;
            DataTable dtCsv = new DataTable();
            string Fulltext;
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    var columnCount = 0;
                    Fulltext = sr.ReadToEnd().ToString(); //read full file text  
                    string[] rows = Fulltext.Split('\n'); //split full file text into rows  
                    for (int i = 0; i < rows.Count() - 1; i++)
                    {
                        string[] rowValues = rows[i].Split(','); //split each row with comma to get individual values  
                        {
                            if (i == 0)
                            {
                                for (int j = 0; j < rowValues.Count(); j++)
                                {
                                    var thisCol = rowValues[j];
                                    dtCsv.Columns.Add(thisCol.Replace("\r", "")); //add headers  
                                    if (filterCols.Split(',').ToList().Contains(thisCol.Replace("\r", "")) || filterCols.Split(',').ToList().Contains(thisCol.Replace("\r", "")))
                                    {
                                        phoneCol = thisCol.Replace("\r", "");
                                    }
                                    columnCount = j;
                                }
                               
                            }
                            else
                            {
                                DataRow dr = dtCsv.NewRow();
                                for (int k = 0; k < rowValues.Count() && k<columnCount; k++)
                                {
                                    dr[k] = LeadsHelpers.ProcessNumber(rowValues[k].Replace("\r", "").ToString());
                                }
                                dtCsv.Rows.Add(dr); //add other rows  
                            }
                        }
                    }
                }
            }
            return dtCsv;
        }
        
        public void CreatedDataTableCSV(DataTable dtDataTable, string newFileName)
        {
            var path = Path.Combine(Server.MapPath("~/Content/DataLoads/"), newFileName + ".csv");
            StreamWriter sw = new StreamWriter(path, false);
            //headers  
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                sw.Write(dtDataTable.Columns[i]);
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);
            foreach (DataRow dr in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(','))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }
        
        private void CreateSaveCsvFile(string newFileName, List<long> UserScrubPhonesList)
        {
            var file = Path.Combine(Server.MapPath("~/Content/DataLoads/"), newFileName + ".csv");
            using (var stream = System.IO.File.CreateText(file))
            {
                string csvRow = string.Format("{0}", "Phone");
                stream.WriteLine(csvRow);
                for (int i = 0; i < UserScrubPhonesList.Count(); i++)
                {
                    csvRow = string.Format("{0}", UserScrubPhonesList[i].ToString());
                    stream.WriteLine(csvRow);
                }
            }
        }

        #endregion

        #endregion

        #region User Credits
        public ActionResult UserCreditLogGrid()
        {
            List<UserCreditLogGridViewModel> retData = new List<UserCreditLogGridViewModel>();
            var userCreditLogs = userCreditLogsService.GetAllUserCreditLogs().Where(x=>x.UserId==CurrentLoggedInUser.Id && x.PackageId>0).ToList();
            int iCount = 0;
            foreach (var u in userCreditLogs)
            {
                iCount += 1;
                retData.Add(new UserCreditLogGridViewModel()
                {
                    SNo = iCount,
                    Id = u.Id,
                    Date = u.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    CreatedAt = u.CreatedAt,
                    Credits =LeadsHelpers.ToUsNumberFormat(u.Credits),
                    DisCountPercentage = u.DiscountPercentage.ToString(),
                    AmountPaid = Math.Round(u.FinalAmount,2).ToString(),
                    PackageName = creditPackageService.GetAllCreditPackages().FirstOrDefault(x => x.Id == u.PackageId).PackageName
                });
            }
            var userCreditLogs2 = userCreditLogsService.GetAllUserCreditLogs().Where(x => x.UserId == CurrentLoggedInUser.Id && x.PackageId == 0).ToList();
            foreach (var u in userCreditLogs2)
            {
                iCount += 1;
                retData.Add(new UserCreditLogGridViewModel()
                {
                    SNo = iCount,
                    Id = u.Id,
                    Date = u.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    CreatedAt = u.CreatedAt,
                    Credits = LeadsHelpers.ToUsNumberFormat(u.Credits),
                    DisCountPercentage = "-",
                    AmountPaid = "-",
                    PackageName ="Admin Provided Credits"
                });
            }
            var usersRefered = userService.GetUsers().Where(x => x.ReferedUserId.HasValue && x.ReferedUserId.Value == CurrentLoggedInUser.Id).ToList();
            var referalCreditsList = userCreditLogsService.GetAllUserCreditLogs().ToList().Where(x => usersRefered.Select(y=>y.Id).Contains(x.UserId));
            foreach (var u in referalCreditsList)
            {
                iCount += 1;
                var thisUser = usersRefered.Where(x => x.Id == u.UserId).FirstOrDefault();
                retData.Add(new UserCreditLogGridViewModel()
                {
                    SNo = iCount,
                    Id = u.Id,
                    Date = u.CreatedAt.ToString("dd-MMM-yyyy hh:mm:ss tt"),
                    CreatedAt = u.CreatedAt,
                    Credits =LeadsHelpers.ToUsNumberFormat(u.ReferalUserCredits),
                    DisCountPercentage = "-",
                    AmountPaid = "-",
                    PackageName = "Referral Bonus From "+ thisUser.Name
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

        [HttpPost]
        public ActionResult AddUserCredits(int packageId, int qty, long credits, float amount, int discountPercentage, float finalAmount, long referalCredits,string transactionDetails)
        {
            long rCredits = 0;
            if (CurrentLoggedInUser.ReferedUserId.HasValue && CurrentLoggedInUser.ReferedUserId.Value > 0)
            {
                rCredits = referalCredits;
                var userData = userService.GetUsers().Where(x => x.Id == CurrentLoggedInUser.ReferedUserId.Value).FirstOrDefault();
                userData.CreditScore += referalCredits;
                userService.UpdateUserDetails(userData);
            }
            userCreditLogsService.BuyCredits(CurrentLoggedInUser.Id, packageId, qty, credits, Convert.ToInt64(amount), discountPercentage, finalAmount, rCredits, transactionDetails);
            CurrentLoggedInUser.CreditScore += credits;
            userService.UpdateUserDetails(CurrentLoggedInUser);
            return Json("");
        }
        #endregion
    }

}