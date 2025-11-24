namespace CleanArchitecture.ApiTemplate.Core.Domain.Enums
{
    /// <summary>
    /// Represents the lifecycle status of a user account in the system.
    /// </summary>
    /// <remarks>
    /// User status determines what operations can be performed on the account
    /// and controls access to system features.
    /// 
    /// Status Transitions:
    /// ```
    ///     ????????????????????????????????????????
    ///     ?           [Registration]             ?
    ///     ?                                      ?
    ///  ??????????   Deactivate   ????????????   ?
    ///  ? Active ? ?????????????? ? Inactive ?   ?
    ///  ??????????                 ????????????   ?
    ///     ? ?                         ?          ?
    ///     ? ? Reactivate             ?          ?
    ///     ? ???????????????????????????          ?
    ///     ?                                      ?
    ///     ? Suspend                              ?
    ///     ?                                      ?
    ///  ?????????????   Lift Suspension          ?
    ///  ? Suspended ? ??????????????????????????  ?
    ///  ?????????????                             ?
    ///     ?                                      ?
    ///     ? Lock                                 ?
    ///     ?                                      ?
    ///  ??????????                                ?
    ///  ? Locked ?                                ?
    ///  ??????????                                ?
    ///     ?                                      ?
    ///     ? Unlock (Admin)                       ?
    ///     ????????????????????????????????????????
    /// ```
    /// 
    /// Business Rules:
    /// - Active: Full system access, can perform all operations
    /// - Inactive: Voluntarily disabled by user, can be reactivated
    /// - Suspended: Temporarily disabled by admin, requires approval to reactivate
    /// - Locked: Security measure after failed login attempts, requires admin unlock
    /// 
    /// Security Implications:
    /// - Only Active users can log in
    /// - Locked users require admin intervention
    /// - Suspended users need compliance review
    /// - Inactive users maintain data but cannot access system
    /// </remarks>
    public enum UserStatus
    {
        /// <summary>
        /// User account is active and fully functional.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Can log in and access all authorized features
        /// - Can perform all operations based on assigned roles
        /// - Default status for new user registrations
        /// - No restrictions on account usage
        /// 
        /// Common Operations:
        /// - Login authentication ?
        /// - Password reset ?
        /// - Profile updates ?
        /// - Feature access ?
        /// </remarks>
        Active = 1,

        /// <summary>
        /// User account is temporarily deactivated by the user.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - User voluntarily disabled their account
        /// - Cannot log in but data is preserved
        /// - Can be reactivated by user request
        /// - Useful for temporary leave or privacy concerns
        /// 
        /// Common Operations:
        /// - Login authentication ?
        /// - Self-reactivation via email ?
        /// - Data retention ?
        /// - Notification opt-out ?
        /// 
        /// Use Cases:
        /// - User taking a break from the platform
        /// - Privacy-conscious users
        /// - Seasonal account usage
        /// - "Pause" functionality
        /// </remarks>
        Inactive = 2,

        /// <summary>
        /// User account is suspended by an administrator.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Administrative action required to suspend
        /// - Cannot log in until unsuspended
        /// - Usually temporary pending investigation
        /// - Requires reason documentation for compliance
        /// 
        /// Common Operations:
        /// - Login authentication ?
        /// - Admin review required ?
        /// - Appeal process ?
        /// - Audit logging ?
        /// 
        /// Reasons for Suspension:
        /// - Policy violations
        /// - Security concerns
        /// - Payment issues
        /// - Compliance review
        /// - Abuse reports
        /// 
        /// Legal Considerations:
        /// - Must comply with terms of service
        /// - May require notification to user
        /// - Should document suspension reason
        /// - May have appeal process requirements
        /// </remarks>
        Suspended = 3,

        /// <summary>
        /// User account is locked due to security concerns.
        /// </summary>
        /// <remarks>
        /// Characteristics:
        /// - Automatically locked after failed login attempts
        /// - Cannot log in until admin unlocks
        /// - Security measure to prevent brute force attacks
        /// - May trigger security alerts
        /// 
        /// Common Operations:
        /// - Login authentication ?
        /// - Password reset ? (with identity verification)
        /// - Admin unlock ?
        /// - Security audit ?
        /// 
        /// Lock Triggers:
        /// - Multiple failed login attempts (e.g., 5 in 15 minutes)
        /// - Suspicious activity detection
        /// - Compromised password notification
        /// - Manual admin lock for security
        /// 
        /// Unlock Process:
        /// 1. Verify user identity (email, phone, security questions)
        /// 2. Reset password (force password change)
        /// 3. Review recent activity
        /// 4. Admin approval (for sensitive accounts)
        /// 5. Unlock account
        /// 
        /// Security Best Practices:
        /// - Log all lock/unlock events
        /// - Send notifications on lock
        /// - Implement progressive delays
        /// - Consider CAPTCHA on unlock
        /// </remarks>
        Locked = 4
    }
}
