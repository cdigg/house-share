using HouseShare.Domain;
using HouseShare.Domain.Repositories.Abstract;
using HouseShare.Domain.Repositories.Concrete;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(HouseShare.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(HouseShare.App_Start.NinjectWebCommon), "Stop")]

namespace HouseShare.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;

    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            kernel.Bind<HouseShareEntities>().ToSelf().InRequestScope();
            kernel.Bind<IShareEntity>().To<DbShareEntity>().InRequestScope();
            kernel.Bind<IShareEntityDate>().To<DbShareEntityDate>().InRequestScope();
            kernel.Bind<IMoneyTransaction>().To<DbMoneyTransaction>().InRequestScope();
            kernel.Bind<IOwe>().To<DbOwe>().InRequestScope();
            kernel.Bind<IPayment>().To<DbPayment>().InRequestScope();
            kernel.Bind<IPurchase>().To<DbPurchase>().InRequestScope();
            kernel.Bind<IUserProfile>().To<DbuserProfile>().InRequestScope();
            kernel.Bind<IHouse>().To<DbHouse>().InRequestScope();
        }        
    }
}
