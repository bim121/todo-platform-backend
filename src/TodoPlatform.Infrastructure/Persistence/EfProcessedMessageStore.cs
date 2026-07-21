using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class EfProcessedMessageStore(AppDbContext db) : IProcessedMessageStore
{
    public async Task<bool> TryAcquireAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken))
            return false;

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }
}
