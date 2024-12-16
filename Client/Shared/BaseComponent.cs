﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyVideoResume.Client.Services;
using Radzen;
using Microsoft.AspNetCore.Components.Authorization;
using System.ComponentModel;
using System.Linq;
using MyVideoResume.Web.Common;

namespace MyVideoResume.Client.Shared;


public static class EnumExtensions
{
    static public SortedList<string, string> ToSortedList(this Enum enumValue)
    {
        var field = enumValue.GetType().GetFields();
        var y = field.Where(x =>
        {
            var attributes = x.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes != null && attributes.Length > 0)
                return true;
            return false;
        }).Select(x =>
        {
            var attributes = x.GetCustomAttributes(typeof(DescriptionAttribute), false);
            var key = string.Empty;
            var value = string.Empty;
            value = (attributes.FirstOrDefault() as DescriptionAttribute).Description;
            key = x.Name;
            return new KeyValuePair<string, string>(key, value);
        }).ToDictionary();
        return new SortedList<string, string>(y);
    }
}

public class BaseComponent : LayoutComponentBase
{
    [Inject] protected MenuService MenuService { get; set; }

    [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    [Inject] protected HttpClient Http { get; set; }

    [Inject] protected NavigationManager NavigationManager { get; set; }

    [Inject] protected IJSRuntime JSRuntime { get; set; }

    [Inject] protected DialogService DialogService { get; set; }

    [Inject] protected TooltipService TooltipService { get; set; }

    [Inject] protected ContextMenuService ContextMenuService { get; set; }

    [Inject] protected NotificationService NotificationService { get; set; }

    [Inject] protected SecurityWebService Security { get; set; }

    public void ShowTooltip(ElementReference elementReference, string content) => TooltipService.Open(elementReference, content, new TooltipOptions() { Position = TooltipPosition.Top });

    public void NavigateTo(string path)
    {
        NavigationManager.NavigateTo(path);
    }

    public void NavigateTo(string path, string parameter)
    {
        NavigationManager.NavigateTo($"{path}/{parameter}");
    }

    public Type? ResolveComponent(string componentName, string namespacevalue)
    {
        return string.IsNullOrEmpty(componentName) ? null
            : Type.GetType($"{namespacevalue}.{componentName}");
    }
}
