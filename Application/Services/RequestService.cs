using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class RequestService : ServiceBase
    {
        public RequestService(AppDbContext context) : base(context)
        {
        }

        public async Task CreateRequest(Request request)
        {
            await context.Requests.AddAsync(request);
            await context.SaveChangesAsync();
        }

        public async Task<Request> GetRequestByIdAsync(int id)
        {
            return await context.Requests.Include(x => x.AssignedTo).FirstAsync(x => x.Id == id);
        }

        public async Task<Request> RevokeRequestByIdAsync(int id)
        {
            var request = await GetRequestByIdAsync(id);
            request.AssignedToId = null;
            request.Status = Status.Новая;
            return request;
        }

        public async Task<Request> DoneRequestByIdAsync(int id)
        {
            var request = await GetRequestByIdAsync(id);
            request.Status = Status.Готово;
            return request;
        }

        public async Task<List<Request>> GetRequestsAsync()
        {
            return await context.Requests.Where(x => x.Status == Status.Новая).OrderByDescending(x => x.CreatedAt).Include(x => x.CreatedBy).ToListAsync();
        }
    }
}
