using System.Collections.Generic;

namespace MiKiNuo.Mvi.Infrastructure.BuildTime.SourceGeneration;

/// <summary>
/// 表示 <see cref="MviDiContainerGenerator"/> 的组合装配数据模型部分。
/// </summary>
public sealed partial class MviDiContainerGenerator
{
    internal static partial class Models
    {
        /// <summary>
        /// 表示一个 [MviComposition] 声明的组合装配模型。
        /// </summary>
        public sealed class CompositionInfo
        {
            /// <summary>
            /// 初始化组合装配模型。
            /// </summary>
            /// <param name="name">组合名称（声明类名）。</param>
            /// <param name="typeName">组合句柄类型完整限定名。</param>
            /// <param name="members">成员实例装配信息集合。</param>
            /// <param name="routes">请求路由接线集合。</param>
            /// <param name="subscriptions">通知订阅接线集合。</param>
            public CompositionInfo(
                string name,
                string typeName,
                IReadOnlyList<CompositionMemberInfo> members,
                IReadOnlyList<CompositionRouteInfo> routes,
                IReadOnlyList<CompositionSubscriptionInfo> subscriptions)
            {
                Name = name;
                TypeName = typeName;
                Members = members;
                Routes = routes;
                Subscriptions = subscriptions;
            }

            /// <summary>组合名称（声明类名）。</summary>
            public string Name { get; }

            /// <summary>组合句柄类型完整限定名。</summary>
            public string TypeName { get; }

            /// <summary>成员实例装配信息集合。</summary>
            public IReadOnlyList<CompositionMemberInfo> Members { get; }

            /// <summary>请求路由接线集合。</summary>
            public IReadOnlyList<CompositionRouteInfo> Routes { get; }

            /// <summary>通知订阅接线集合。</summary>
            public IReadOnlyList<CompositionSubscriptionInfo> Subscriptions { get; }
        }

        /// <summary>
        /// 表示组合内一个成员 Feature 的实例装配信息。
        /// </summary>
        public sealed class CompositionMemberInfo
        {
            /// <summary>
            /// 初始化成员实例装配信息。
            /// </summary>
            /// <param name="feature">成员 Feature 装配模型。</param>
            /// <param name="variableName">生成代码中的局部变量名。</param>
            /// <param name="propertyName">组合句柄上的属性名。</param>
            public CompositionMemberInfo(
                Models.MviFeatureInfo feature,
                string variableName,
                string propertyName)
            {
                Feature = feature;
                VariableName = variableName;
                PropertyName = propertyName;
            }

            /// <summary>成员 Feature 装配模型。</summary>
            public Models.MviFeatureInfo Feature { get; }

            /// <summary>生成代码中的局部变量名。</summary>
            public string VariableName { get; }

            /// <summary>组合句柄上的属性名。</summary>
            public string PropertyName { get; }
        }

        /// <summary>
        /// 表示一条请求路由接线：唯一提供方注册处理器，消费方绑定到提供方。
        /// </summary>
        public sealed class CompositionRouteInfo
        {
            /// <summary>
            /// 初始化请求路由接线。
            /// </summary>
            /// <param name="requestTypeName">请求契约类型完整限定名。</param>
            /// <param name="responseTypeName">响应类型完整限定名。</param>
            /// <param name="provider">提供方成员。</param>
            /// <param name="handlerMethodName">提供方处理方法名。</param>
            /// <param name="consumers">消费方成员集合。</param>
            public CompositionRouteInfo(
                string requestTypeName,
                string responseTypeName,
                CompositionMemberInfo provider,
                string handlerMethodName,
                IReadOnlyList<CompositionMemberInfo> consumers)
            {
                RequestTypeName = requestTypeName;
                ResponseTypeName = responseTypeName;
                Provider = provider;
                HandlerMethodName = handlerMethodName;
                Consumers = consumers;
            }

            /// <summary>请求契约类型完整限定名。</summary>
            public string RequestTypeName { get; }

            /// <summary>响应类型完整限定名。</summary>
            public string ResponseTypeName { get; }

            /// <summary>提供方成员。</summary>
            public CompositionMemberInfo Provider { get; }

            /// <summary>提供方处理方法名。</summary>
            public string HandlerMethodName { get; }

            /// <summary>消费方成员集合。</summary>
            public IReadOnlyList<CompositionMemberInfo> Consumers { get; }
        }

        /// <summary>
        /// 表示一条通知订阅接线。
        /// </summary>
        public sealed class CompositionSubscriptionInfo
        {
            /// <summary>
            /// 初始化通知订阅接线。
            /// </summary>
            /// <param name="notificationTypeName">通知契约类型完整限定名。</param>
            /// <param name="subscriber">订阅方成员。</param>
            /// <param name="acceptorMethodName">订阅方接纳方法名。</param>
            public CompositionSubscriptionInfo(
                string notificationTypeName,
                CompositionMemberInfo subscriber,
                string acceptorMethodName)
            {
                NotificationTypeName = notificationTypeName;
                Subscriber = subscriber;
                AcceptorMethodName = acceptorMethodName;
            }

            /// <summary>通知契约类型完整限定名。</summary>
            public string NotificationTypeName { get; }

            /// <summary>订阅方成员。</summary>
            public CompositionMemberInfo Subscriber { get; }

            /// <summary>订阅方接纳方法名。</summary>
            public string AcceptorMethodName { get; }
        }
    }
}
