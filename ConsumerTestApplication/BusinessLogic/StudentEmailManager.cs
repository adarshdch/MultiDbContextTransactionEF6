﻿using System;
using System.Collections.Generic;
using ConsumerTestApplication.DatabaseContexts;
using ConsumerTestApplication.DomainModel;
using MultiDbContextTransactionEF6.Core.Enums;
using MultiDbContextTransactionEF6.Core.Interface;

namespace ConsumerTestApplication.BusinessLogic
{
	
public class StudentEmailManager
	{
		private readonly IDbContextScopeFactory _dbContextScopeFactory;

		public StudentEmailManager(IDbContextScopeFactory dbContextScopeFactory)
		{
			if (dbContextScopeFactory == null) throw new ArgumentNullException("dbContextScopeFactory");
			_dbContextScopeFactory = dbContextScopeFactory;
		}

		public void SendWelcomeEmail(Guid studentId)
		{
			/*
			 * Demo of forcing the creation of a new DbContextScope
			 * to ensure that changes made to the model in this service 
			 * method are persisted even if that method happens to get
			 * called within the scope of a wider business transaction
			 * that eventually fails for any reason.
			 * 
			 * This is an advanced feature that should be used as rarely 
			 * as possible (and ideally, never).
			 */

			// We're going to send a welcome email to the provided student
			// (if one hasn't been sent already). Once sent, we'll update
			// that Student entity in our DB to record that its Welcome email
			// has been sent.

			// Emails can't be rolled-back. Once they're sent, they're sent. 
			// So once the email has been sent successfully, we absolutely 
			// must persist this fact in our DB. Even if that method is called
			// by another busines logic service method as part of a wider 
			// business transaction and even if that parent business transaction
			// ends up failing for any reason, we still must ensure that
			// we have recorded the fact that the Welcome email has been sent.
			// Otherwise, we would risk spamming our students with repeated Welcome
			// emails. 

			// Force the creation of a new DbContextScope so that the changes we make here are
			// guaranteed to get persisted regardless of what happens after this method has completed.
			using (var dbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew))
			{
				var dbContext = dbContextScope.DbContexts.Get<SchoolDbContext>();
				var student = dbContext.Students.Find(studentId);

				if (student == null)
					throw new ArgumentException(String.Format("Invalid studentId provided: {0}. Couldn't find a Student with this ID.", studentId));

				if (!student.WelcomeEmailSent)
				{
					SendEmail(student.Email);
					student.WelcomeEmailSent = true;
				}

				dbContextScope.SaveChanges();

				// When you force the creation of a new DbContextScope, you must force the parent
				// scope (if any) to reload the entities you've modified here. Otherwise, the method calling
				// you might not be able to see the changes you made here.
				dbContextScope.RefreshEntitiesInParentScope(new List<Student> {student});
			}
		}

		private void SendEmail(string emailAddress)
		{
			// Send the email synchronously. Throw if any error occurs.
			// [...]
		}
	}
}
