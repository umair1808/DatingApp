using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
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

        public void AddGroup(Group group)
        {
            _context.Groups.Add(group);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _context.Connections.FindAsync(connectionId);
        }

        public void RemoveConnection(Connection connection)
        {
            _context.Connections.Remove(connection);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _context.Groups
            .Include(g => g.Connections)
            .FirstOrDefaultAsync(g => g.Name == groupName);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _context.Groups
            .Include(g => g.Connections)
            .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
            .FirstOrDefaultAsync();
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
                        .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                        .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(m => m.RecipientUsername == messageParams.Username && m.RecipientDeleted == false),
                "Outbox" => query.Where(m => m.SenderUsername == messageParams.Username && m.SenderDeleted == false),
                 _ => query.Where(m => m.RecipientUsername == messageParams.Username && m.DateRead == null && m.RecipientDeleted == false)
            };

            return await PagedList<MessageDto>.CreateAsync(query, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsename, string recipentUsername)
        {
            var messages = await _context.Messages
            .Where(m => m.Recipient.UserName == currentUsename && m.RecipientDeleted == false
                    && m.Sender.UserName == recipentUsername
                    || m.Recipient.UserName == recipentUsername && m.SenderDeleted == false
                    && m.Sender.UserName == currentUsename
            )
            .MarkUnreadAsRead(currentUsename)
            .OrderBy(m => m.MessageSent)
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && m.RecipientUsername== currentUsename).ToList();

            // unreadMessages.ForEach(m => m.DateRead = DateTime.Now); Not recommended from code readablity perspective 
                
            if(unreadMessages.Any()){
                foreach(var message in unreadMessages){
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return messages;
        }
    }
}