﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebStore.DAL.Context;
using WebStore.Domain.Entities;
using WebStore.Infrastructure.Interfaces;
using WebStore.Models.Cart;
using WebStore.Models.Order;
namespace WebStore.Infrastructure.Implementations.Sql
{
	public class SqlOrdersService : IOrdersService
	{
		private readonly WebStoreContext _context;
		private readonly UserManager<User> _userManager;

		public SqlOrdersService(WebStoreContext context, UserManager<User> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public IEnumerable<Order> GetUserOrders(string userName)
		{
			return _context.Orders.Include("User").Include("OrderItems").Where(o => o.User.UserName.Equals(userName)).ToList();
		}

		public Order GetOrderById(int id)
		{
			return _context.Orders.Include("OrderItems").FirstOrDefault(o => o.Id.Equals(id));
		}

		public Order CreateOrder(OrderViewModel orderModel, CartViewModel transformCart, string userName)
		{
			var user = _userManager.FindByNameAsync(userName).Result;

			using (var transaction = _context.Database.BeginTransaction())
			{
				var order = new Order()
				{
					Address = orderModel.Address,
					Name = orderModel.Name,
					Date = DateTime.Now,
					Phone = orderModel.Phone,
					User = user
				};
				_context.Orders.Add(order);

				foreach (var item in transformCart.Items)
				{
					var productVm = item.Key;
					var product = _context.Products.FirstOrDefault(p => p.Id.Equals(productVm.Id));
					if (product == null)
						throw new InvalidOperationException("Product is missing from database");
					var orderItem = new OrderItem()
					{
						Order = order,
						Price = product.Price,
						Quantity = item.Value,
						Product = product
					};
					_context.OrderItems.Add(orderItem);
				}

				_context.SaveChanges();
				transaction.Commit();

				return order;
			}
		}
	}
}