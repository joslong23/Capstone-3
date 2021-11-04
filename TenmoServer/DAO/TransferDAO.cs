﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TenmoServer.Models;

namespace TenmoServer.DAO
{
    public class TransferDAO : ITransferDAO
    {
        private readonly string connStr;

        private string SqlGetOtherUser = "";

        private readonly string SqlGetTransfers =
            "SELECT t.transfer_id, t.transfer_status_id, t.account_from, t.account_to, t.amount, a.account_id AS user_account_id, " +
            "(SELECT DISTINCT username FROM transfers t " +
            "INNER JOIN accounts a ON(a.account_id = t.account_from OR a.account_id = t.account_to) " +
            "INNER JOIN users u ON u.user_id = a.user_id " +
            "WHERE (a.account_id != t.account_from OR a.account_id != t.account_to) " +
            "AND u.user_id != @user_id) AS other_username " +
            "FROM transfers t INNER JOIN accounts a ON(a.account_id = t.account_from OR a.account_id = t.account_to) " +
            "INNER JOIN users u ON u.user_id = a.user_id " +
            "WHERE a.user_id = @user_id";

        public TransferDAO(string connStr)
        {
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new ArgumentNullException(nameof(connStr));
            }
            this.connStr = connStr;
        }

        public List<Transfer> GetTransfers(int userId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(SqlGetTransfers, conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<Transfer> transfers = new List<Transfer>();
                        while (reader.Read())
                        {
                            int statusInt = Convert.ToInt32(reader["transfer_status_id"]);
                            int userAccountId = Convert.ToInt32(reader["user_account_id"]);
                            Transfer transfer = new Transfer
                            {
                                TransferId = Convert.ToInt32(reader["transfer_id"]),
                                TransferStatus = SetTransferStatus(statusInt),
                                Amount = Convert.ToDecimal(reader["amount"]),
                                AccountFrom = Convert.ToInt32(reader["account_from"]),
                                AccountTo = Convert.ToInt32(reader["account_to"]),
                                OtherUser = Convert.ToString(reader["other_username"]),
                            };
                            transfer.TransferType = SetTransferType(transfer.AccountTo, userAccountId);
                            transfers.Add(transfer);
                        }
                        return transfers;
                    }
                }
            }
        }

        public string SetTransferType(int accountToId, int userAccountId)
        {
            if (accountToId == userAccountId)
            {
                return "From";
            }
            else
            {
                return "To";
            }
        }

        public string SetTransferStatus(int statusInt)
        {
            switch (statusInt)
            {
                case 2000:
                    return "Pending";
                case 2001:
                    return "Approved";
                case 2002:
                    return "Rejected";
                default:
                    return null;
            }
        }
    }
}
