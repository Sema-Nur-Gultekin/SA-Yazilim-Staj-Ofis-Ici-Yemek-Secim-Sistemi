using System;
using ofis_ici_yemek_secim_sistemi.Data;
using ofis_ici_yemek_secim_sistemi.Models;

namespace ofis_ici_yemek_secim_sistemi.Services
{

    public static class ActivityLogger
    {
     
        public static void Log(AppDbContext context, int companyId, int userId, string actionName, int? affectedRecordId = null)
        {
            if (context == null || userId <= 0 || string.IsNullOrWhiteSpace(actionName))
                return; 

            context.ActivityLogs.Add(new ActivityLog
            {
                CompanyID = companyId,
                UserID = userId,
                ActionName = actionName,
                AffectedRecordID = affectedRecordId,
                ActionTime = DateTime.Now
            });
        }


        public static void LogAndSave(AppDbContext context, int companyId, int userId, string actionName, int? affectedRecordId = null)
        {
            Log(context, companyId, userId, actionName, affectedRecordId);
            context.SaveChanges();
        }
    }
}
