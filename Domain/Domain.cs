﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace RentalHelper.Domain
{
    public class User
    {
        public long TelegramId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public ICollection<Request> Requests { get; set; } = new List<Request>();
        
        public Role Role { get; set; }

        public uState UserState { get; set; }
    }

    public class Tenant : User
    {
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
    public class Worker : User
    { }
    public class Admin : User
    { }
    public enum uState
    {
        NewUser,
        Idle,
        SelectionRole,
        TenantCreatingRequest,
        TenantGivingAccess,
        TenantDeniyngAccess,
        TenantBookingRoom,
        WorkerTakeRequest,
        WorkerDoneRequest,
        AdminRequestInfo,
        AdminSetPrice,
    }
    public enum Role
    {
        НовыйПользователь,
        Арендатор,
        Сотрудник,
        Админ
    }

    public class Request
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
        public long CreatedById { get; set; }
        public Tenant CreatedBy { get; set; } = null!;
        public long? AssignedToId { get; set; }
        public Worker? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum Status
    {
        Новая,
        Выполняется,
        Готово
    }

    

    public class Vehicle
    {
        public int Id { get; set; }
        public string PlateNumber { get; set; }
        public Tenant User { get; set; }

        public int? Price { get; set; }
        public long UserId { get; set; }
        public bool IsPaid { get; set; }
        public bool IsOneTime { get; set; }
    }

    public class Booking
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public Tenant User { get; set; }
        public long UserId { get; set; }
    }
}
