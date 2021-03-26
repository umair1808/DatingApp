using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages
            .Include(x => x.Recipient)
            .Include(x => x.Sender)
            .SingleOrDefaultAsync(x => x.Id == id);
            
            //.FindAsync(id); cannot be used to Include any related entities
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages
                        .OrderByDescending(m => m.MessageSent)
                        .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(m => m.Recipient.Username == messageParams.Username && m.RecipientDeleted == false),
                "Outbox" => query.Where(m => m.Sender.Username == messageParams.Username && m.SenderDeleted == false),
                _ => query.Where(m => m.Recipient.Username == messageParams.Username && m.DateRead == null && m.RecipientDeleted == false)
            };
            
            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);

        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsename, string recipentUsername)
        {
            var messages = await _context.Messages
            .Include(u => u.Sender).ThenInclude(p => p.Photos)
            .Include(u => u.Recipient).ThenInclude(p => p.Photos)
            .Where(m => m.Recipient.Username == currentUsename && m.RecipientDeleted == false
                    && m.Sender.Username == recipentUsername
                    || m.Recipient.Username == recipentUsername && m.SenderDeleted == false
                    && m.Sender.Username == currentUsename
            ).OrderBy(m => m.MessageSent).ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.Recipient.Username == currentUsename).ToList();

            // unreadMessages.ForEach(m => m.DateRead = DateTime.Now); Not recommended from code readablity perspective 
                
            if(unreadMessages.Any()){
                foreach(var message in unreadMessages){
                    message.DateRead = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}