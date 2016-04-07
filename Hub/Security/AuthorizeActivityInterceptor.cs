﻿using System;
using System.Linq;
using System.Reflection;
using System.Web;
using Castle.Core.Interceptor;
using Data.Infrastructure.Security;
using Data.Infrastructure.StructureMap;
using StructureMap;

namespace Hub.Security
{
    public class AuthorizeActivityInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            AuthorizeActivity(invocation);
        }

        private void AuthorizeActivity(IInvocation invocation)
        {
            if (!IsMethodMarkedForAuthorization(invocation.MethodInvocationTarget))
            {
                invocation.Proceed();
                return;
            }

            var authorizeAttribute = (invocation.Method.GetCustomAttributes(typeof(AuthorizeActivityAttribute), true).First()
                as AuthorizeActivityAttribute ?? new AuthorizeActivityAttribute());

            var objectArgumentIndex = authorizeAttribute.ObjectIdArgumentIndex;

            var parameter = invocation.GetArgumentValue(objectArgumentIndex);

            Guid objectId = Guid.Empty;
            if (parameter is Guid)
            {
                objectId = (Guid) parameter;
            }
            else
            {
                var property = parameter.GetType().GetProperty("Id");
                objectId = (Guid) property.GetValue(parameter);
            }

            ISecurityServices securityServices = ObjectFactory.GetInstance<ISecurityServices>();
            if (securityServices.AuthorizeActivity(authorizeAttribute.Privilege, objectId))
            {
                invocation.Proceed();
            }
            else
            {
                throw new HttpException(401, "You are not authorized to perform this activity!");
            }
        }

        private bool IsMethodMarkedForAuthorization(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(typeof(AuthorizeActivityAttribute), true).Any();
        }
    }
}
