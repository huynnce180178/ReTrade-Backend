sequenceDiagram
    actor Admin
    participant Page as Admin Dashboard Page
    participant Ctrl as :AdminDashboardController
    participant Svc as :AdminDashboardService
    participant MySvcRepo as :MyServiceRepository
    participant SubRepo as :ServiceSubscriptionRepository
    participant DB as Database

    Admin->>Page: 1. Navigate to Subscription Statistics
    activate Page
    
    Page->>Ctrl: 2. GET /api/AdminDashboard/subscription-statistics
    activate Ctrl
    
    Ctrl->>Svc: 3. GetSubscriptionStatisticsAsync()
    activate Svc
    
    Svc->>MySvcRepo: 4. Query()
    activate MySvcRepo
    MySvcRepo->>DB: 5. Execute SELECT MyService
    activate DB
    DB-->>MySvcRepo: 6. Return MyService data
    deactivate DB
    MySvcRepo-->>Svc: 7. Return IQueryable<MyService>
    deactivate MySvcRepo
    
    Svc->>SubRepo: 8. Query()
    activate SubRepo
    SubRepo->>DB: 9. Execute SELECT ServiceSubscription
    activate DB
    DB-->>SubRepo: 10. Return ServiceSubscription data
    deactivate DB
    SubRepo-->>Svc: 11. Return IQueryable<ServiceSubscription>
    deactivate SubRepo
    
    Svc->>Svc: Calculate subscription statistics
    Svc->>Svc: Map to SubscriptionStatisticsDto
    
    Svc-->>Ctrl: 12. Return SubscriptionStatisticsDto
    deactivate Svc
    
    Ctrl-->>Page: 13. Return HTTP 200 + Response JSON
    deactivate Ctrl
    
    Page-->>Admin: 14. Display statistics and charts
    deactivate Page
