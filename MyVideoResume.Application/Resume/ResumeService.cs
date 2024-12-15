﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyVideoResume.Abstractions.Core;
using MyVideoResume.Abstractions.Resume;
using MyVideoResume.Data;
using MyVideoResume.Data.Models.Resume;
using System.Text.Json;

namespace MyVideoResume.Application.Resume;

public class ResumeService
{
    private readonly ILogger<ResumeService> _logger;
    private readonly DataContext _dataContext;
    private readonly IConfiguration _configuration;

    public ResumeService(ILogger<ResumeService> logger, IConfiguration configuration, DataContext context)
    {
        _dataContext = context;
        _logger = logger;
        _configuration = configuration;
    }

    //Get All Public Resume Summaries
    public async Task<List<ResumeSummaryItem>> GetResumeSummaryItems(string? userId = null, bool? onlyPublic = null)
    {
        var result = new List<ResumeSummaryItem>();
        try
        {
            var query = _dataContext.Resumes.AsNoTracking()
                        .Include(x => x.Basics)
                       .Include(x => x.ResumeInformation).ThenInclude(x => x.MetaData)
                       .Include(x => x.ResumeInformation).ThenInclude(x => x.UserProfile)
                       .Include(x => x.ResumeInformation).ThenInclude(x => x.ResumeTemplate)
                        .Where(x => x.DeletedDateTime == null);

            if (onlyPublic.HasValue)
            {
                query = query.Where(x => x.ResumeInformation.Privacy_ShowResume == DisplayPrivacy.ToPublic);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(x => x.UserId == userId);
            }

            result = query.Select(x => new ResumeSummaryItem() { UserId = x.UserId, CreationDateTimeFormatted = x.CreationDateTime.Value.ToString("yyyy-MM-dd"), IsPublic = true, Id = x.Id.ToString(), ResumeTemplateName = x.ResumeInformation.ResumeTemplate.Name, ResumeSummary = x.Basics.Summary, ResumeSlug = x.ResumeInformation.Slug, ResumeName = x.Basics.Name }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        return result;
    }

    public async Task<ResponseResult> Save(string userId, ResumeInformationEntity resume)
    {

        return new ResponseResult();
    }

    private IQueryable<ResumeInformationEntity> GetResume()
    {
        var items = _dataContext.ResumeInformation
            .Include(x => x.MetaResume)
            .Include(x => x.MetaResume).ThenInclude(x => x.Publications)
            .Include(x => x.MetaResume).ThenInclude(x => x.Projects)
            .Include(x => x.MetaResume).ThenInclude(x => x.Languages)
            .Include(x => x.MetaResume).ThenInclude(x => x.Work)
            .Include(x => x.MetaResume).ThenInclude(x => x.Awards)
            .Include(x => x.MetaResume).ThenInclude(x => x.References)
            .Include(x => x.MetaResume).ThenInclude(x => x.Basics)
            .Include(x => x.MetaResume).ThenInclude(x => x.Certificates)
            .Include(x => x.MetaResume).ThenInclude(x => x.Education)
            .Include(x => x.MetaResume).ThenInclude(x => x.Interests)
            .Include(x => x.MetaResume).ThenInclude(x => x.Volunteer)
            .Include(x => x.MetaResume).ThenInclude(x => x.Skills)
            .Include(x => x.MetaData)
            .Include(x => x.UserProfile)
            .Include(x => x.ResumeTemplate)
            .AsNoTracking();
        return items;
    }

    public async Task<ResumeInformationEntity> GetResume(string resumeId)
    {
        var result = new ResumeInformationEntity();
        try
        {
            //Is it a slug?
            result = GetResume().FirstOrDefault(x => x.Slug == resumeId);
            if (result== null)
            {
                result = GetResume().FirstOrDefault(x => x.Id == Guid.Parse(resumeId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        return result;
    }

    public async Task<List<ResumeInformationEntity>> GetResumes(string userId)
    {
        var result = new List<ResumeInformationEntity>();
        try
        {
            result = await GetResume().Where(x => x.UserId == userId).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
        return result;
    }

    public async Task<ResponseResult> DeleteResume(string userId, string resumeId)
    {

        var result = new ResponseResult();
        result.ErrorMessage = "Failed to Delete Resume";
        try
        {
            var userProfile = _dataContext.UserProfiles.Include(x => x.ResumeItems).FirstOrDefault(x => x.UserId == userId);
            if (userProfile != null)
            {
                var resume = _dataContext.ResumeInformation.Include(x => x.MetaResume).FirstOrDefault(x => x.Id == Guid.Parse(resumeId));
                if (resume != null)
                {
                    //Check if the User owns the resume
                    if (resume.UserId == userId)
                    {
                        resume.Privacy_ShowContactDetails = DisplayPrivacy.ToSelf;
                        resume.Privacy_ShowResume = DisplayPrivacy.ToSelf;
                        resume.UserId = string.Empty;
                        resume.DeletedDateTime = DateTime.UtcNow;
                        resume.MetaResume.UserId = string.Empty;
                        resume.MetaResume.DeletedDateTime = DateTime.UtcNow;
                        userProfile.ResumeItems.Remove(resume);
                        _dataContext.SaveChanges();
                        result.Result = "Operation Successful";
                        result.ErrorMessage = string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            result.ErrorMessage = ex.Message;
        }
        return result;
    }

    public async Task<ResponseResult> CreateResume(string userId, string resumeText)
    {
        var result = new ResponseResult() { };

        try
        {
            if (!string.IsNullOrWhiteSpace(userId)) //should validate that its a real user account...
            {
                var profile = _dataContext.UserProfiles.Include(x => x.ResumeItems).FirstOrDefault(x => x.UserId == userId);
                if (profile != null)
                {
                    if (profile.ResumeItems == null)
                        profile.ResumeItems = new List<ResumeInformationEntity>();

                    // Create the standard template
                    var standardTemplate = _dataContext.ResumeTemplates.FirstOrDefault(x => x.TransformerComponentName == "StandardTemplate");
                    if (standardTemplate == null)
                    {
                        standardTemplate = ResumeTemplateEntity.CreateStandardResumeTemplate();
                        standardTemplate.UserId = userId;
                        _dataContext.ResumeTemplates.Add(standardTemplate);
                        _dataContext.SaveChanges();
                    }

                    //Save the Resume to get an object to populate into 
                    var tempresume = JsonSerializer.Deserialize<MetaResumeEntity>(resumeText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var resumeInformation = new ResumeInformationEntity() { CreationDateTime = DateTime.UtcNow, UserId = userId, ResumeSerialized = resumeText, Name = tempresume.Basics.Name, Privacy_ShowResume = DisplayPrivacy.ToPublic, Privacy_ShowContactDetails = DisplayPrivacy.ToPublic, ResumeTemplate = standardTemplate, MetaResume = tempresume };
                    tempresume.UserId = userId;
                    tempresume.CreationDateTime = DateTime.UtcNow;
                    profile.ResumeItems.Add(resumeInformation);
                    _dataContext.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            result.ErrorMessage = ex.Message;
        }
        return result;
    }
}
