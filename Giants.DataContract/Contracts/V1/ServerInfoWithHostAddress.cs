﻿namespace Giants.DataContract.V1
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class ServerInfoWithHostAddress : ServerInfo
    {
        [Required]
        public string HostIpAddress { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ServerInfoWithHostAddress address &&
                   base.Equals(obj) &&
                   this.HostIpAddress == address.HostIpAddress;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(this.HostIpAddress);
            return hash.ToHashCode();
        }
    }
}
