﻿using Dinner.Models.Home;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Dinner.Services
{
    public class HomeService
    {
        List<PaymentDetail> _payments;
        User[] _users;

        public List<PaymentDetail> Payments()
        {
            if (_payments != null)
                return _payments;

            List<SavePayment> savePayments;
            var paymentspath = HostingEnvironment.MapPath("~/App_Data/payments.json");
            if (!File.Exists(paymentspath))
            {
                savePayments = new List<SavePayment>();
                File.WriteAllText(paymentspath, JsonConvert.SerializeObject(savePayments));
            }

            savePayments = JsonConvert.DeserializeObject<List<SavePayment>>(File.ReadAllText(paymentspath));
            return _payments = ToPaymentDetails(savePayments);
        }

        public void AddPayment(string currentUserName, PaymentViewModel payment)
        {
            var pays = JsonConvert.DeserializeObject<List<FormPayment>>(payment.Data);

            _payments.Add(new PaymentDetail {
                Date = DateTime.Now,
                Payer = GetCurrentUser(currentUserName),
                Payments = pays.Select(it => new UserPayment { User = GetUserById(it.id), Amount = it.sum } ).ToArray()
            });

            var paymentspath = HostingEnvironment.MapPath("~/App_Data/payments.json");
            File.WriteAllText(paymentspath, JsonConvert.SerializeObject(ToSavePayments(_payments)));
        }

        public User[] GetUsers()
        {
            if (_users != null)
                return _users;

            var userspath = HostingEnvironment.MapPath("~/App_Data/users.json");
            if (!File.Exists(userspath)) {

                _users = new User[] {
                    new User{ Id = 1, Name = "Михаил", Password = "12345678"},
                    new User{ Id = 2, Name = "Илья", Password = "12345678"},
                    new User{ Id = 3, Name = "Юра", Password = "12345678"},
                    new User{ Id = 4, Name = "Андрей", Password = "12345678"}
                };
                File.WriteAllText(userspath, JsonConvert.SerializeObject(_users));
                return _users;
            }

            return _users = JsonConvert.DeserializeObject<User[]>(File.ReadAllText(userspath));
        }

        public User GetCurrentUser(string name)
        {
            return GetUsers().First(it => it.Name == name);
        }

        public User GetUserById(int id)
        {
            return GetUsers().First(it => it.Id == id);
        }


        public List<Debt> GetDebts()
        {
            var result = new List<Debt>();

            foreach (var user in GetUsers().OrderBy(usr => usr.Id))
            {
                foreach (var user2 in GetUsers().Where(usr => usr.Id > user.Id).OrderBy(usr => usr.Id))
                {
                    result.Add(new Debt
                    {
                        From = user2,
                        To = user,
                        Amount = CalcDebt(user2, user)
                    });
                }
            }

            return result;
        }


        public decimal CalcDebt(User from, User to)
        {
            var fpart = _payments.Where(it => it.Payer.Id == to.Id).Sum(it => it.Payments.FirstOrDefault(pay => pay.User.Id == from.Id)?.Amount ?? 0);
            var spart = _payments.Where(it => it.Payer.Id == from.Id).Sum(it => it.Payments.FirstOrDefault(pay => pay.User.Id == to.Id)?.Amount ?? 0);
            return fpart - spart;
        }



        public List<PaymentDetail> ToPaymentDetails(List<SavePayment> savePayments)
        {
            return savePayments.Select(pay => new PaymentDetail
            {
                Date = pay.date,
                Payer = GetUserById(pay.payerId),
                Payments = pay.payments.Select(it => new UserPayment
                {
                    User = GetUserById(it.userId),
                    Amount = it.amount
                }).ToArray()
            }).ToList();
        }


        public List<SavePayment> ToSavePayments(List<PaymentDetail> payments)
        {
            return payments.Select(pay => new SavePayment
            {
                date = pay.Date,
                payerId = pay.Payer.Id,
                payments = pay.Payments.Select(it => new SaveUserPayment
                {
                    userId = it.User.Id,
                    amount = it.Amount
                }).ToArray()
            }).ToList();
        }
    }
}