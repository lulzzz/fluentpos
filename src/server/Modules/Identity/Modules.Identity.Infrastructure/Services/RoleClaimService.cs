﻿using AutoMapper;
using FluentPOS.Modules.Identity.Core.Abstractions;
using FluentPOS.Modules.Identity.Core.Entities;
using FluentPOS.Modules.Identity.Infrastructure.Persistence;
using FluentPOS.Shared.Core.Wrapper;
using FluentPOS.Shared.DTOs.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentPOS.Shared.Core.Constants;

namespace FluentPOS.Modules.Identity.Infrastructure.Services
{
    public class RoleClaimService : IRoleClaimService
    {
        private readonly IStringLocalizer<RoleClaimService> _localizer;
        private readonly IMapper _mapper;
        private readonly IdentityDbContext _db;

        public RoleClaimService(
            IStringLocalizer<RoleClaimService> localizer,
            IMapper mapper,
            IdentityDbContext db)
        {
            _localizer = localizer;
            _mapper = mapper;
            _db = db;
        }

        public async Task<Result<List<RoleClaimResponse>>> GetAllAsync()
        {
            var roleClaims = await _db.RoleClaims.AsNoTracking().ToListAsync();
            var roleClaimsResponse = _mapper.Map<List<RoleClaimResponse>>(roleClaims);
            return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
        }

        public async Task<int> GetCountAsync()
        {
            var count = await _db.RoleClaims.AsNoTracking().CountAsync();
            return count;
        }

        public async Task<Result<RoleClaimResponse>> GetByIdAsync(int id)
        {
            var roleClaim = await _db.RoleClaims.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id);
            var roleClaimResponse = _mapper.Map<RoleClaimResponse>(roleClaim);
            return await Result<RoleClaimResponse>.SuccessAsync(roleClaimResponse);
        }

        public async Task<Result<List<RoleClaimResponse>>> GetAllByRoleIdAsync(string roleId)
        {
            var roleClaims = await _db.RoleClaims
                .AsNoTracking()
                .Include(x => x.Role)
                .Where(x => x.RoleId == roleId)
                .ToListAsync();
            var roleClaimsResponse = _mapper.Map<List<RoleClaimResponse>>(roleClaims);
            return await Result<List<RoleClaimResponse>>.SuccessAsync(roleClaimsResponse);
        }

        public async Task<Result<string>> SaveAsync(RoleClaimRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleId))
            {
                return await Result<string>.FailAsync(_localizer["Role is required."]);
            }

            if (request.Id == 0)
            {
                var existingRoleClaim =
                    await _db.RoleClaims
                        .SingleOrDefaultAsync(x =>
                            x.RoleId == request.RoleId && x.ClaimType == request.Type && x.ClaimValue == request.Value);
                if (existingRoleClaim != null)
                {
                    return await Result<string>.FailAsync(_localizer["Similar Role Claim already exists."]);
                }
                var roleClaim = _mapper.Map<FluentRoleClaim>(request);
                await _db.RoleClaims.AddAsync(roleClaim);
                await _db.SaveChangesAsync();
                return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} created."], request.Value));
            }
            else
            {
                var existingRoleClaim =
                    await _db.RoleClaims
                        .Include(x => x.Role)
                        .SingleOrDefaultAsync(x => x.Id == request.Id);
                if (existingRoleClaim == null)
                {
                    return await Result<string>.SuccessAsync(_localizer["Role Claim does not exist."]);
                }
                else
                {
                    existingRoleClaim.ClaimType = request.Type;
                    existingRoleClaim.ClaimValue = request.Value;
                    existingRoleClaim.Group = request.Group;
                    existingRoleClaim.Description = request.Description;
                    existingRoleClaim.RoleId = request.RoleId;
                    _db.RoleClaims.Update(existingRoleClaim);
                    await _db.SaveChangesAsync();
                    return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} for Role {1} updated."], request.Value, existingRoleClaim.Role.Name));
                }
            }
        }

        public async Task<Result<string>> DeleteAsync(int id)
        {
            var existingRoleClaim = await _db.RoleClaims
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (existingRoleClaim != null)
            {
                if (existingRoleClaim.Role?.Name == RoleConstants.SuperAdmin)
                {
                    return await Result<string>.FailAsync(string.Format(_localizer["Not allowed to delete Permissions for {0} Role."], existingRoleClaim.Role.Name));
                }

                _db.RoleClaims.Remove(existingRoleClaim);
                await _db.SaveChangesAsync();
                return await Result<string>.SuccessAsync(string.Format(_localizer["Role Claim {0} for {1} Role deleted."], existingRoleClaim.ClaimValue, existingRoleClaim.Role?.Name));
            }
            else
            {
                return await Result<string>.FailAsync(_localizer["Role Claim does not exist."]);
            }
        }
    }
}