﻿using API.DTOs;
using API.Entity;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext context;
        private readonly IMapper mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<AppUser>> GetAllAsync()
        {
            return await context.Users
                .Include(p => p.Photos)
                .ToListAsync();
        }

        public async Task<MemberDto> GetMemberAsync(string username)
        {
            return await context.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDto>( mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<PageList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var query = context.Users.AsQueryable();

            query = query.Where(u => u.UserName != userParams.CurrentUsername);
            query = query.Where(u => u.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge -1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge -1);

            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            } ;
            return await PageList<MemberDto>.CreateAsync(
                query.AsNoTracking().ProjectTo<MemberDto>(mapper.ConfigurationProvider),
                userParams.PageNumber, userParams.PageSize);
                
        }

        public async Task<AppUser> GetUserByUserIdAsync(int id)
        {
            return await context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await context.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public void Update(AppUser user)
        {
            context.Entry(user).State = EntityState.Modified;
        }
    }
}
