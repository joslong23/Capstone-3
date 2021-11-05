﻿using System;
using System.Collections.Generic;
using TenmoClient.Data;
using TenmoClient.Models;

namespace TenmoClient
{
    public class UserInterface
    {
        private readonly AuthService authService = new AuthService();
        private readonly DisplayHelper display = new DisplayHelper();
        private TransferApi transferApi;
        private RequestHandler requestHandler;
        private bool quitRequested = false;

        public void Start()
        {
            while (!quitRequested)
            {
                while (!UserService.IsLoggedIn)
                {
                    ShowLogInMenu();
                }

                // If we got here, then the user is logged in. Go ahead and show the main menu
                ShowMainMenu();
            }
        }

        private void ShowLogInMenu()
        {
            Console.WriteLine("Welcome to TEnmo!");
            Console.WriteLine("1: Login");
            Console.WriteLine("2: Register");
            Console.Write("Please choose an option: ");

            if (!int.TryParse(Console.ReadLine(), out int loginRegister))
            {
                Console.WriteLine("Invalid input. Please enter only a number.");
            }
            else if (loginRegister == 1)
            {
                HandleUserLogin();
            }
            else if (loginRegister == 2)
            {
                HandleUserRegister();
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }

        private void ShowMainMenu()
        {
            List<User> users = authService.GetUsers();
            List<Transfer> transfers = new List<Transfer>();
            int menuSelection;
            do
            {
                Console.WriteLine();
                Console.WriteLine("Welcome to TEnmo! Please make a selection: ");
                Console.WriteLine("1: View your current balance");
                Console.WriteLine("2: View all transfers");
                Console.WriteLine("3: Find transfer by ID");
                Console.WriteLine("4: View your pending requests");
                Console.WriteLine("5: Send TE bucks");
                Console.WriteLine("6: Request TE bucks");
                Console.WriteLine("7: Log in as different user");
                Console.WriteLine("0: Exit");
                Console.WriteLine("---------");
                Console.Write("Please choose an option: ");

                if (!int.TryParse(Console.ReadLine(), out menuSelection))
                {
                    Console.WriteLine("Invalid input. Please enter only a number.");
                }
                else
                {
                    Console.WriteLine();
                    switch (menuSelection)
                    {
                        case 1: // View Balance
                            decimal balance = transferApi.GetBalance(UserService.UserId);
                            Console.WriteLine("Your Account Balance: " + balance.ToString("C"));
                            break;

                        case 2: // View All Transfers
                            transfers = transferApi.GetTransfers(UserService.UserId);
                            if (transfers.Count > 0)
                            {
                                display.DisplayTransferList(users, transfers);
                            }
                            break;

                        case 3: // View Specific Transfer
                            int transferId = ConsoleService.PromptForTransferID("search");
                            Transfer transfer = transferApi.GetTransferById(UserService.UserId, transferId);
                            if (!(transfer == null))
                            {
                                display.DisplayTransfer(users, transfer);
                            }
                            break;

                        case 4: // View Pending Requests
                            if (transfers.Count < 1)
                            {
                                transfers = transferApi.GetTransfers(UserService.UserId);
                            }
                            List<Transfer> pendingTransfers = new List<Transfer>();
                            foreach (Transfer t in transfers)
                            {
                                if (t.TransferStatus == 2000)
                                {
                                    pendingTransfers.Add(t);
                                }
                            }
                            if (pendingTransfers.Count > 0)
                            {
                                display.DisplayPendingTransferList(users, pendingTransfers);
                                ManageTransfers(pendingTransfers);
                            }
                            else
                            {
                                Console.WriteLine("You have no pending transfers.");
                            }
                            break;

                        case 5: // Send TE Bucks
                            display.DisplayUsersList(users);
                            transfer = ConsoleService.GetTransferDetails(true);
                            transfer = transferApi.CreateTransfer(transfer, UserService.UserId);
                            if (transfer != null)
                            {
                                display.DisplayTransfer(users, transfer);
                            }
                            break;

                        case 6: // Request TE Bucks
                            display.DisplayUsersList(users);
                            transfer = ConsoleService.GetTransferDetails(false);
                            transfer = transferApi.CreateTransfer(transfer, UserService.UserId);
                            display.DisplayTransfer(users, transfer); // TODO: Implement me
                            break;

                        case 7: // Log in as someone else
                            Console.WriteLine();
                            UserService.ClearLoggedInUser(); //wipe out previous login info
                            return; // Leaves the menu and should return as someone else

                        case 0: // Quit
                            Console.WriteLine("Goodbye!");
                            quitRequested = true;
                            return;

                        default:
                            Console.WriteLine("That doesn't seem like a valid choice.");
                            break;
                    }
                }
            } while (menuSelection != 0);
        }

        private void ManageTransfers(List<Transfer> pendingTransfers)
        {
            bool leaveMenu = false;
            while (!leaveMenu)
            {
                int transferId = ConsoleService.PromptForTransferID("manage");
                Transfer selectedTransfer = new Transfer();
                foreach (Transfer t in pendingTransfers)
                {
                    if (t.TransferId == transferId)
                    {
                        selectedTransfer = t;
                    }
                }
                if (selectedTransfer != null)
                {
                    selectedTransfer = requestHandler.ManagePendingRequest(selectedTransfer);
                    transferApi.UpdateTransfer(selectedTransfer, UserService.UserId);
                }
                else
                {
                    Console.WriteLine("Invalid transfer ID");
                    Console.WriteLine();
                }

                leaveMenu = ConsoleService.GetBool("Are you finished managing transactions?(Y/N) ");
            }
        }

        private void HandleUserRegister()
        {
            bool isRegistered = false;

            while (!isRegistered) //will keep looping until user is registered
            {
                LoginUser registerUser = ConsoleService.PromptForLogin();
                isRegistered = authService.Register(registerUser);
            }

            Console.WriteLine("");
            Console.WriteLine("Registration successful. You can now log in.");
        }

        private void HandleUserLogin()
        {
            while (!UserService.IsLoggedIn) //will keep looping until user is logged in
            {
                LoginUser loginUser = ConsoleService.PromptForLogin();

                if (authService.Login(loginUser))
                {
                    transferApi = new TransferApi(UserService.Token);
                }
            }
        }
    }
}
