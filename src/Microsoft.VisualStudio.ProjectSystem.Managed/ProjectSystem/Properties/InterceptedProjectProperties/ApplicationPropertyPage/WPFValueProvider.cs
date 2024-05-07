﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.WPF;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportInterceptingPropertyValueProvider(
    new[]
    {
        StartupURIPropertyName,
        ShutdownModePropertyName
    },
    ExportInterceptingPropertyValueProviderFile.ProjectFile)]
internal class WPFValueProvider : InterceptingPropertyValueProviderBase
{
    internal const string StartupURIPropertyName = "StartupURI";
    internal const string ShutdownModePropertyName = "ShutdownMode_WPF";
    internal const string UseWPFPropertyName = "UseWPF";
    internal const string OutputTypePropertyName = "OutputType";
    internal const string WinExeOutputTypeValue = "WinExe";

    private readonly IApplicationXamlFileAccessor _applicationXamlFileAccessor;

    [ImportingConstructor]
    public WPFValueProvider(IApplicationXamlFileAccessor applicationXamlFileAccessor)
    {
        _applicationXamlFileAccessor = applicationXamlFileAccessor;
    }

    public override Task<string> OnGetEvaluatedPropertyValueAsync(string propertyName, string evaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName, defaultProperties);
    }

    public override Task<string> OnGetUnevaluatedPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties)
    {
        return GetPropertyValueAsync(propertyName, defaultProperties);
    }

    public override async Task<string?> OnSetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string>? dimensionalConditions = null)
    {
        if (await IsWPFApplicationAsync(defaultProperties))
        {
            await (propertyName switch
            {
                StartupURIPropertyName => _applicationXamlFileAccessor.SetStartupUriAsync(unevaluatedPropertyValue),
                ShutdownModePropertyName => _applicationXamlFileAccessor.SetShutdownModeAsync(unevaluatedPropertyValue),

                _ => throw new InvalidOperationException($"The {nameof(WPFValueProvider)} does not support the '{propertyName}' property.")
            });
        }

        return null;
    }

    private async Task<string> GetPropertyValueAsync(string propertyName, IProjectProperties defaultProperties)
    {
        if (await IsWPFApplicationAsync(defaultProperties))
        {
            return propertyName switch
            {
                StartupURIPropertyName => await _applicationXamlFileAccessor.GetStartupUriAsync() ?? string.Empty,
                ShutdownModePropertyName => await _applicationXamlFileAccessor.GetShutdownModeAsync() ?? "OnLastWindowClose",

                _ => throw new InvalidOperationException($"The {nameof(WPFValueProvider)} does not support the '{propertyName}' property.")
            };
        }

        return string.Empty;
    }

    private static async Task<bool> IsWPFApplicationAsync(IProjectProperties defaultProperties)
    {
        string useWPFString = await defaultProperties.GetEvaluatedPropertyValueAsync(UseWPFPropertyName);
        string outputTypeString = await defaultProperties.GetEvaluatedPropertyValueAsync(OutputTypePropertyName);

        return bool.TryParse(useWPFString, out bool useWPF)
            && useWPF
            && StringComparers.PropertyLiteralValues.Equals(outputTypeString, WinExeOutputTypeValue);
    }
}
