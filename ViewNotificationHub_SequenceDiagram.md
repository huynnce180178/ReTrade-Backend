sequenceDiagram
    actor User
    participant Page as Notification Page / Dropdown
    participant Hub as :NotificationHub
    participant Svc as :NotificationService
    participant AccRepo as :AccountRepository
    participant NotifRepo as :NotificationRepository
    participant DB as DB

    User->>Page: 1. Click Bell Icon (Notification Dropdown)
    Page->>Hub: 2. Invoke GetMyNotifications() via SignalR
    
    Hub->>Hub: Get accountId from Context
    
    Hub->>Svc: 3. GetMyNotificationsAsync(accountId)
    
    Svc->>AccRepo: 4. Query()
    AccRepo->>DB: 5. Execute SELECT query
    DB-->>AccRepo: 6. Return Data
    AccRepo-->>Svc: 7. Return IQueryable<Account>
    
    Svc->>Svc: Account valid?
    alt Account not found
        Svc-->>Hub: 8.1.1. Return empty list
        Hub-->>Page: 8.1.2. Return empty data via SignalR
        Page-->>User: 8.1.3. Show message: "Account not found."
    else Account found
        Svc->>NotifRepo: 8.2.1. Query()
        NotifRepo->>DB: 8.2.2. Execute SELECT query
        DB-->>NotifRepo: 8.2.3. Return Data
        NotifRepo-->>Svc: 8.2.4. Return IQueryable<Notification>
        
        Svc->>Svc: 8.2.5. Map to NotificationDto List
        
        Svc->>Svc: Notifications found?
        alt Notifications found
            Svc-->>Hub: 8.2.6. Return List<NotificationDto>
            Hub-->>Page: 8.2.7. Return data via SignalR
            Page-->>User: 8.2.8. Display Notifications
        else No notifications found
            Svc-->>Hub: 8.3.1. Return empty list
            Hub-->>Page: 8.3.2. Return empty data via SignalR
            Page-->>User: 8.3.3. Show message: "No notifications found."
        end
    end
