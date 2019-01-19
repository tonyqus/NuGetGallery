﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.AzureSearch.SearchService
{
    public interface ISearchTextBuilder
    {
        string V2Search(V2SearchRequest request);
        string V3Search(V3SearchRequest request);
    }
}