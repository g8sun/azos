/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Azos.Log;
using Azos.Glue.Protocol;
using Azos.Security;


namespace Azos.Glue.Implementation
{
    /// <summary>
    /// Executes server work - handles requests synchronously or asynchronously.
    /// </summary>
    public class ServerHandler : GlueComponent
    {
       #region CONSTS

           public const int DEFAULT_INSTANCE_TIMEOUT_MS =
                                                      5 * //min
                                                      60 * //sec
                                                      1000; //msec


           /// <summary>
           /// Specifies special signature for glue-specific constructors.
           /// If a server implementer class implements a public ctor with this signature then it will be called
           /// instead of default .ctor.
           /// This is useful when server class needs to distinguish Glue allocations from de-serializations and others
           /// </summary>
           public static readonly Type[] GLUE_CTOR_SIGNATURE = new Type[]
                                     {
                                        typeof(IGlue),
                                        typeof(ServerInstanceMode)
                                     };



       #endregion

       #region .ctor
          public ServerHandler(IGlueImplementation glue) : base(glue)
          {

          }
       #endregion


       #region Fields

           //note: they are kept as instance fields per ServerHandler instance, not static!!!
           private object m_SingletonInstancesLock = new object();
           private volatile Dictionary<Type, object> m_SingletonInstances;

       #endregion


     #region Public

        /// <summary>
        /// Handles request in the context of ServerHandler thread, replying back to result queue
        /// </summary>
        public void HandleRequestAsynchronously(RequestMsg request)
        {
          //todo In Future may supply Request.LongRunning to better predict thread allocation
          Task.Factory.StartNew(
                (r) => {
                    var req = (RequestMsg)r;

                    ResponseMsg response;
                    try
                    {
                        response = HandleRequestSynchronously(req);
                    }
                    catch (Exception e1)
                    {
                        try
                        {
                            //call goes via Glue because there may be some global event handlers
                            response = Glue.ServerHandleRequestFailure(req.RequestID, req.OneWay, e1, req.BindingSpecificContext);
                        }
                        catch(Exception e2)
                        {
                            this.WriteLog(LogSrc.Server,
                                 MessageType.Error,
                                 string.Format(StringConsts.GLUE_SERVER_HANDLER_ERROR + e2.ToMessageWithType()),
                                 from: "SrvrHndlr.HndlReqAsnly(ReqMsg:A)",
                                 exception: e2
                                );
                            return;
                        }
                    }

                    if (!req.OneWay)
                        try
                        {
                            req.ServerTransport.SendResponse(response);
                        }
                        catch(Exception error)
                        {
                            this.WriteLog(LogSrc.Server,
                                 MessageType.Error,
                                 string.Format(StringConsts.GLUE_SERVER_HANDLER_ERROR + error.ToMessageWithType()),
                                 from: "SrvrHndlr.HndlReqAsnly(ReqMsg:B)",
                                 exception: error
                                );
                        }
                },
                request);
        }

        /// <summary>
        /// Handles request synchronously in the context of the calling thread. Returns NULL for one-way calls
        /// </summary>
        public ResponseMsg HandleRequestSynchronously(RequestMsg request)
        {
              try
              {
                 return inspectAndHandleRequest(request);
              }
              catch(Exception error) //nothing may leak
              {
                 if (request.OneWay)
                 {
                    //because it is one-way, the caller will never know about it
                    this.WriteLog(LogSrc.Server,
                                 MessageType.Error,
                                 string.Format(StringConsts.GLUE_SERVER_ONE_WAY_CALL_ERROR + error.ToMessageWithType()),
                                 from: "SrvrHndlr.HandleRequestSynchronously(ReqMsg)",
                                 exception: error
                                );
                     return null;
                 }
                 else
                 {
                     //call goes via Glue because there may be some global event handlers
                     var response = Glue.ServerHandleRequestFailure(request.RequestID, request.OneWay, error, request.BindingSpecificContext);
                     return response;
                 }
              }
              finally
              {
                 Apps.ExecutionContext.__SetThreadLevelSessionContext(null);
              }
        }



        /// <summary>
        /// Handles request synchronously in the context of the calling thread. Returns NULL for one-way calls
        /// </summary>
        public ResponseMsg HandleRequestFailure(FID reqID, bool oneWay, Exception failure, object bindingSpecCtx)
        {
            if (oneWay)
                return null;

            var red = new WrappedExceptionData(failure);
            var response = new ResponseMsg(reqID, red);
            response.__SetBindingSpecificContext(bindingSpecCtx);

            return response;
       }


     #endregion


     #region Protected

            protected override void DoStart()
            {
                base.DoStart();
                m_SingletonInstances = new Dictionary<Type,object>();
            }

            protected override void DoWaitForCompleteStop()
            {
                base.DoWaitForCompleteStop();
                m_SingletonInstances = null;
            }


     #endregion

     #region .pvt



        internal class serverImplementer
        {
            public ServerHandler Handler;
            public ServerEndPoint ServerEndPoint;
            public Type Contract;
            public Type Implementation;
            public ServerInstanceMode InstanceMode;
            public int InstanceTimeoutMs;
            public bool ThreadSafe;
            public bool AuthenticationSupport;
            public bool SupportsGlueCtor;

            public struct mapping
            {
              public mapping(MethodInfo c, MethodInfo i, Func<object, RequestMsg, object> f)
              {
                miContract = c; miImplementation = i; fBody = f;
              }
              public MethodInfo miContract;
              public MethodInfo miImplementation;
              public Func<object, RequestMsg, object> fBody;
            }


            public Dictionary<MethodSpec, mapping> methodMap = new Dictionary<MethodSpec, mapping>();

            public serverImplementer(ServerHandler handler, ServerEndPoint sep, Type contract)
            {
                Handler = handler;
                ServerEndPoint = sep;
                Contract = contract;

                var implementers = sep.ContractServers.Where(ts => contract.IsAssignableFrom(ts)).ToArray();

                if (implementers.Length==0)
                throw new ServerContractException(string.Format(StringConsts.GLUE_ENDPOINT_CONTRACT_NOT_IMPLEMENTED_ERROR, sep.Name, contract.FullName));

                if (implementers.Length>1)
                {
                      handler.WriteLog(LogSrc.Server,
                                       MessageType.Warning,
                                       string.Format(StringConsts.GLUE_ENDPOINT_CONTRACT_MANY_SERVERS_WARNING, contract.FullName, sep.Name),
                                       from: "serverImplementer");
                }

                Implementation = implementers[0];

                //pre-alloc Contract/Implementation method infos
                var intfMapping = Implementation.GetInterfaceMap(Contract);
                foreach(var miContract in Contract.GetMethods())
                {
                  var mspec = new MethodSpec(miContract);
                  var miImpl = intfMapping.TargetMethods.FirstOrDefault( tmi => new MethodSpec(tmi).Equals( mspec ));
                  if (miImpl==null)
                    throw new ServerContractException(StringConsts.GLUE_ENDPOINT_CONTRACT_INTF_MAPPING_ERROR.Args( sep.Name,
                                                                                                                   contract.FullName,
                                                                                                                   miContract.Name,
                                                                                                                   Implementation.FullName));
                  var fBody = getFBody(miContract);
                  methodMap.Add(mspec, new mapping(miContract, miImpl, fBody));//todo DODELAT Functor!
                }

                var lifeCycle = Contract.GetCustomAttributes(typeof(LifeCycleAttribute), false).FirstOrDefault() as LifeCycleAttribute;
                if (lifeCycle!=null)
                {
                    InstanceMode = lifeCycle.Mode;
                    InstanceTimeoutMs = lifeCycle.TimeoutMs;
                }

                if (InstanceTimeoutMs==0) InstanceTimeoutMs = DEFAULT_INSTANCE_TIMEOUT_MS;

                ThreadSafe = Attribute.IsDefined(Implementation, typeof(ThreadSafeAttribute), false);

                AuthenticationSupport =  Attribute.IsDefined(Contract, typeof(AuthenticationSupportAttribute), false);

                SupportsGlueCtor = Implementation.GetConstructor(ServerHandler.GLUE_CTOR_SIGNATURE) != null;


            }

            public mapping SpecToMethodInfos(MethodSpec spec)
            {
                mapping result;
                if (methodMap.TryGetValue(spec, out result)) return result;

                throw new ServerContractException(StringConsts.GLUE_ENDPOINT_MSPEC_NOT_FOUND_ERROR.Args( ServerEndPoint.Name,
                                                                                                         Contract.FullName,
                                                                                                         spec.ToString()));
            }

            private Func<object, RequestMsg, object> getFBody(MethodInfo miContract)
            {
               var attr = miContract.GetCustomAttribute(typeof(ArgsMarshallingAttribute)) as ArgsMarshallingAttribute;
               if (attr==null) return null;

               var tReqMsg = attr.RequestMsgType;

                var pInstance = Expression.Parameter(typeof(object));
                var pMsg = Expression.Parameter(typeof(RequestMsg));
                var pCastMsg = Expression.Variable(tReqMsg);

                var exprArgs = new List<Expression>();
                var pars = miContract.GetParameters();
                for(var i=0; i<pars.Length; i++)
                {
                   var par = pars[i];
                   if (par.ParameterType.IsByRef || par.IsOut || par.ParameterType.IsGenericParameter)
                     throw new ServerContractException(StringConsts.GLUE_METHOD_SPEC_UNSUPPORTED_ERROR.Args(miContract.DeclaringType.FullName, miContract.Name, par.Name));

                   var fName = "MethodArg_{0}_{1}".Args(i, par.Name);
                   exprArgs.Add( Expression.Field(pCastMsg, fName) );
                }

               try
               {
                 var isVoid = miContract.ReturnType==typeof(void);

                 if (isVoid)
                 {
                     return Expression.Lambda<Func<object, RequestMsg, object>>(
                                        Expression.Block(
                                                 new ParameterExpression[]{pCastMsg},
                                                 Expression.Assign(pCastMsg, Expression.Convert( pMsg, tReqMsg ) ),
                                                 Expression.Call( Expression.Convert( pInstance, Contract), miContract, exprArgs.ToArray()),
                                                 Expression.Constant(null, typeof(object))
                                         ),//block
                                         pInstance, pMsg)
                                      .Compile();
                 }
                 else
                 {
                     return Expression.Lambda<Func<object, RequestMsg, object>>(
                                        Expression.Block(
                                                 new ParameterExpression[]{pCastMsg},
                                                 Expression.Assign(pCastMsg, Expression.Convert( pMsg, tReqMsg ) ),
                                                 Expression.Convert(
                                                    Expression.Call( Expression.Convert( pInstance, Contract), miContract, exprArgs.ToArray()),
                                                    typeof(object)
                                                 )
                                         ),//block
                                         pInstance, pMsg)
                                      .Compile();
                 }
               }
               catch(Exception error)
               {
                 throw new ServerContractException(StringConsts.GLUE_METHOD_ARGS_MARSHAL_LAMBDA_ERROR.Args(miContract.DeclaringType.FullName, miContract.Name, error.ToMessageWithType()), error);
               }
            }

        }//class serverImplementer -------------------------------------------------------------------------------------------------------







                private ResponseMsg inspectAndHandleRequest(RequestMsg request)
                {
                        //Glue level inspectors
                        var inspectors = Glue.ServerMsgInspectors;
                        for(var i=0; i<inspectors.Count; i++)
                        {
                            var insp = inspectors[i];
                            if (insp==null) continue;
                            request = insp.ServerDispatchRequest(request.ServerTransport.ServerEndpoint, request);
                        }

                        //Binding level inspectors
                        inspectors = request.ServerTransport.Binding.ServerMsgInspectors;
                        for(var i=0; i<inspectors.Count; i++)
                        {
                            var insp = inspectors[i];
                            if (insp==null) continue;
                            request = insp.ServerDispatchRequest(request.ServerTransport.ServerEndpoint, request);
                        }

                        //Endpoint level inspectors
                        inspectors = request.ServerTransport.ServerEndpoint.MsgInspectors;
                        for(var i=0; i<inspectors.Count; i++)
                        {
                            var insp = inspectors[i];
                            if (insp==null) continue;
                            request = insp.ServerDispatchRequest(request.ServerTransport.ServerEndpoint, request);
                        }

                      var response = handleRequest(request);


                      if (!request.OneWay && response!=null)
                      {

                        //Glue level inspectors
                        inspectors = Glue.ServerMsgInspectors;
                        for(var i=0; i<inspectors.Count; i++)
                        {
                            var insp = inspectors[i];
                            if (insp==null) continue;
                            response = insp.ServerReturnResponse(request.ServerTransport.ServerEndpoint, request, response);
                        }

                        //Binding level inspectors
                        inspectors = request.ServerTransport.Binding.ServerMsgInspectors;
                        for(var i=0; i<inspectors.Count; i++)
                        {
                            var insp = inspectors[i];
                            if (insp==null) continue;
                            response = insp.ServerReturnResponse(request.ServerTransport.ServerEndpoint, request, response);
                        }

                        //Endpoint level inspectors
                        inspectors = request.ServerTransport.ServerEndpoint.MsgInspectors;
                        for(var i=0; i<inspectors.Count; i++)
                        {
                            var insp = inspectors[i];
                            if (insp==null) continue;
                            response = insp.ServerReturnResponse(request.ServerTransport.ServerEndpoint, request, response);
                        }

                        return response;
                     }

                     return null;
                }


                private ResponseMsg handleRequest(RequestMsg request)
                {
                   try
                   {
                     ServerCall.__SetThreadLevelContext(Glue, request);
                     try
                     {
                       var response = doWork(request);

                       var rhdr = ServerCall.GetResponseHeadersOrNull();

                       if (rhdr!=null && response!=null)
                        response.Headers = rhdr;

                       return response;
                     }
                     finally
                     {
                       ServerCall.__ResetThreadLevelContext();
                     }
                   }
                   catch(Exception error)
                   {
                     if (request.OneWay)
                     {    //because it is one-way, the caller will never know about it
                          this.WriteLog(LogSrc.Server,
                                       MessageType.Error,
                                       string.Format(StringConsts.GLUE_SERVER_ONE_WAY_CALL_ERROR + error.ToMessageWithType()),
                                       from: "SrvrHndlr.handleRequest(ReqMsg)",
                                       exception: error
                                       );
                         return null;
                     }
                     else
                     {
                         var red = new WrappedExceptionData(error);
                         var response = new ResponseMsg(request.RequestID, red);
                         response.__SetBindingSpecificContext(request);
                         return response;
                     }
                   }
                }

                private ResponseMsg doWork(RequestMsg request)
                {
                   var contract = request.Contract;//this throws when contract can't be found
                   var server = getServerImplementer(request.ServerTransport.ServerEndpoint, contract);//throws when no implementor match found

                   if (server.AuthenticationSupport)
                        interpretAuthenticationHeader(request);


                   //Authorizes user to the whole server contract and implementing class
                   Permission.AuthorizeAndGuardAction(App.SecurityManager, server.Contract);
                   Permission.AuthorizeAndGuardAction(App.SecurityManager, server.Implementation);

                   serverImplementer.mapping mapped = server.SpecToMethodInfos(request.Method);


                   Permission.AuthorizeAndGuardAction(App.SecurityManager, mapped.miContract);
                   Permission.AuthorizeAndGuardAction(App.SecurityManager, mapped.miImplementation);


                   Guid? checkedOutID;
                   bool lockTaken;
                   var instance = getServerInstance(server, request, out checkedOutID, out lockTaken); //throws when instance expired or cant be locked
                   try
                   {
                       Guid? instanceID = null;
                       bool isCtor = false;
                       bool isDctor = false;

                       if (server.InstanceMode == ServerInstanceMode.Stateful ||
                           server.InstanceMode == ServerInstanceMode.AutoConstructedStateful)
                       {
                          instanceID = request.RemoteInstance;
                          isCtor = Attribute.IsDefined(mapped.miContract, typeof(ConstructorAttribute));
                          isDctor= Attribute.IsDefined(mapped.miContract, typeof(DestructorAttribute));


                          if (isCtor && isDctor)
                            throw new ServerMethodInvocationException(StringConsts.GLUE_AMBIGUOUS_CTOR_DCTOR_DEFINITION_ERROR
                                                                                  .Args( contract.FullName, request.MethodName));

                          if (server.InstanceMode != ServerInstanceMode.AutoConstructedStateful &&
                              !instanceID.HasValue &&
                              !isCtor)
                            throw new ServerMethodInvocationException(StringConsts.GLUE_NO_SERVER_INSTANCE_ERROR
                                                                                  .Args( contract.FullName, request.MethodName));
                       }

                       //========================================================================================================
                       object result;
                       try
                       {
                            var any = request as RequestAnyMsg;
                            if (any!=null)
                              result = mapped.miContract.Invoke(instance, any.Arguments); //do actual contract-implementing method work
                            else
                            {
                             //call functor using typed RequestMsg derivative
                             if (mapped.fBody==null)
                              throw new ServerMethodInvocationException(StringConsts.GLUE_NO_ARGS_MARSHAL_LAMBDA_ERROR
                                                                                  .Args( contract.FullName, request.MethodName));
                             result = mapped.fBody(instance, request); //do actual contract-implementing method work via Lambda
                            }
                       }
                       catch(Exception bodyError)
                       {
                            Exception err = bodyError;
                            if (err is TargetInvocationException)//unwrap the inner error which is wrapped by Invoke()
                              if (err.InnerException!=null) err = err.InnerException;

                            throw new ServerMethodInvocationException(StringConsts.GLUE_SERVER_CONTRACT_METHOD_INVOCATION_ERROR
                                                                                  .Args(contract.FullName, request.MethodName, err.ToMessageWithType()),
                                                                      err);
                       }
                       //========================================================================================================

                       if (server.InstanceMode == ServerInstanceMode.Stateful ||
                           server.InstanceMode == ServerInstanceMode.AutoConstructedStateful)
                       {
                           if (isCtor || (server.InstanceMode == ServerInstanceMode.AutoConstructedStateful && !isDctor && !instanceID.HasValue))
                           {
                              instanceID = Guid.NewGuid();
                              App.ObjectStore.CheckIn(instanceID.Value, instance, server.InstanceTimeoutMs);
                           }
                           else
                           if (isDctor)
                           {
                             if (instanceID.HasValue)
                             {
                               App.ObjectStore.Delete(instanceID.Value);
                               instanceID = null;
                               checkedOutID = null;
                             }
                           }
                       }


                       if (request.OneWay) return null;

                       var response = new ResponseMsg(request.RequestID, instanceID, result);
                       response.__SetBindingSpecificContext(request);
                       return response;
                   }
                   finally
                   {
                     if (lockTaken)
                        Monitor.Exit(instance);
                     if (checkedOutID.HasValue)
                        App.ObjectStore.CheckIn(checkedOutID.Value, server.InstanceTimeoutMs);
                   }
                }


                private serverImplementer getServerImplementer(ServerEndPoint sep, Type contract)
                {
                  serverImplementer server;
                  if (sep.m_ContractImplementers.TryGetValue(contract, out server)) return server;

                  server = new serverImplementer(this, sep, contract); //throws
                  sep.m_ContractImplementers.TryAdd(contract, server);
                  return server;
                }


                private object getServerInstance(serverImplementer server, RequestMsg request, out Guid? checkedOutID, out bool lockTaken)
                {
                   object result = null;
                   checkedOutID = null;
                   lockTaken = false;

                   if (server.InstanceMode == ServerInstanceMode.Singleton)
                   {
                     if (!m_SingletonInstances.TryGetValue(server.Implementation, out result))//per ServerHandler/Glue instance
                       lock(m_SingletonInstancesLock)
                       {
                         if (!m_SingletonInstances.TryGetValue(server.Implementation, out result))
                         {
                           result = createInstance(server);

                           //Inject dependencies into singleton server implementor ONLY ONCE at creation
                           App.DependencyInjector.InjectInto(result);

                           var dict = new Dictionary<Type, object>( m_SingletonInstances );
                           dict[server.Implementation] = result;
                           Thread.MemoryBarrier();
                           m_SingletonInstances = dict;//atomic
                         }
                       }
                   }
                   else
                   if (server.InstanceMode == ServerInstanceMode.Stateful ||
                       server.InstanceMode == ServerInstanceMode.AutoConstructedStateful)
                   {
                     if (request.RemoteInstance.HasValue)
                     {
                       result = App.ObjectStore.CheckOut(request.RemoteInstance.Value);
                       if (result==null || result.GetType()!=server.Implementation)
                        throw new StatefulServerInstanceDoesNotExistException(StringConsts.GLUE_STATEFUL_SERVER_INSTANCE_DOES_NOT_EXIST_ERROR + server.Implementation.FullName);

                       checkedOutID = request.RemoteInstance.Value;

                       if (!server.ThreadSafe)
                       {
                            if (!Monitor.TryEnter(result, this.Glue.ServerInstanceLockTimeoutMs))
                            {
                              App.ObjectStore.CheckIn(checkedOutID.Value);//check it back in because we could not lock it
                              throw new StatefulServerInstanceLockTimeoutException(StringConsts.GLUE_STATEFUL_SERVER_INSTANCE_LOCK_TIMEOUT_ERROR + server.Implementation.FullName);
                            }
                            lockTaken = true;
                       }
                     }
                     else
                     {
                       result = createInstance(server);//no need to lock as instance is brand new
                     }

                     //Inject dependencies into newly created or checked out stateful instance
                     //The DI is thread safe because in the unlikely case of DI injected values change,
                     //DI sets all ref type fields atomically per CLR guarantee
                     App.DependencyInjector.InjectInto(result);
                   }
                   else // ServerInstanceMode.PerCall
                   {
                     result = createInstance(server);

                     //Inject dependencies into newly created per-call instance
                     App.DependencyInjector.InjectInto(result);
                   }

                   //singleton is NOT DI'ed here

                   return result;
                }


                private object createInstance(serverImplementer server)
                {
                   try
                   {
                     if (server.SupportsGlueCtor)
                        return Activator.CreateInstance(server.Implementation, Glue, server.InstanceMode);
                     else
                        return Activator.CreateInstance(server.Implementation, true);
                   }
                   catch(Exception error)
                   {
                     throw new ServerInstanceActivationException(StringConsts.GLUE_SERVER_INSTANCE_ACTIVATION_ERROR + server.Implementation.FullName, error);
                   }
                }


                private void interpretAuthenticationHeader(RequestMsg request)
                {
                   if (!request.HasHeaders)  return;

                   var ah = request.Headers.FirstOrDefault(h => h is AuthenticationHeader) as AuthenticationHeader;

                   if (ah == null) return;

                   if (ah.Credentials==null && !ah.Token.Assigned) return;

                   User user;
                   if (ah.Credentials!=null)
                      user = App.SecurityManager.Authenticate(ah.Credentials);
                   else
                      user = App.SecurityManager.Authenticate(ah.Token);

                   if (Apps.ExecutionContext.HasThreadContextSession)
                       Apps.ExecutionContext.Session.User = user;
                   else
                       Apps.ExecutionContext.__SetThreadLevelSessionContext( App.MakeNewSessionInstance(Guid.NewGuid(), user) );

                }

     #endregion

    }


}
