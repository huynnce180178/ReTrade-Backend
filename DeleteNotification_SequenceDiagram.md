sequenceDiagram
    actor User
    participant Page as Notification Page / Dropdown
    participant Hub as :NotificationHub
    participant Svc as :NotificationService
    participant AccRepo as :AccountRepository
    participant NotifRepo as :NotificationRepository
    participant DB as DB

    User->>Page: 1. Click delete on notification
    Page->>Hub: 2. Invoke DeleteNotification(notificationId) via SignalR
    
    Hub->>Hub: Get accountId from Context
    
    Hub->>Svc: 3. DeleteNotificationAsync(accountId, notificationId)
    
    Svc->>AccRepo: 4. Query()
    AccRepo->>DB: 5. Execute SELECT query
    DB-->>AccRepo: 6. Return Data
    AccRepo-->>Svc: 7. Return IQueryable<Account>
    
    Svc->>Svc: Account valid?
    
    alt Account invalid
        Svc-->>Hub: 8.1.1. Return false
        Hub-->>Page: 8.1.2. Return error via SignalR
        Page-->>User: 8.1.3. Display Error Message
    else Account exists
        Svc->>NotifRepo: 8.2.1. GetByIdAsync(notificationId)
        NotifRepo->>DB: 8.2.2. Execute SELECT query
        DB-->>NotifRepo: 8.2.3. Return Data
        NotifRepo-->>Svc: 8.2.4. Return Notification entity
        
        Svc->>Svc: Notification valid?
        
        alt Notification valid
            Svc->>Svc: Set IsDeleted = true
            
            Svc->>NotifRepo: 8.2.5. UpdateAsync(notification)
            NotifRepo->>DB: 8.2.6. Execute UPDATE query
            DB-->>NotifRepo: 8.2.7. Return Success
            NotifRepo-->>Svc: 8.2.8. Return Success
            
            Svc-->>Hub: 8.2.9. Return true
            Hub-->>Page: 8.2.10. Return success via SignalR
            Page-->>User: 8.2.11. Visually remove notification
        else Notification invalid or not owned
            Svc-->>Hub: 8.3.1. Return false
            Hub-->>Page: 8.3.2. Return error via SignalR
            Page-->>User: 8.3.3. Display Error Message
        end
    end
