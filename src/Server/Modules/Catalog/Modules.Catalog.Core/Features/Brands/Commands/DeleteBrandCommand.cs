﻿using FluentPOS.Modules.Catalog.Core.Abstractions;
using FluentPOS.Modules.Catalog.Core.Constants;
using FluentPOS.Modules.Catalog.Core.Exceptions;
using FluentPOS.Shared.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPOS.Modules.Catalog.Core.Features.Brands.Commands
{
    public class DeleteBrandCommand : BrandCommand, IRequest<Result<Guid>>
    {
        public DeleteBrandCommand(Guid id)
        {
            Id = id;
        }
    }
}