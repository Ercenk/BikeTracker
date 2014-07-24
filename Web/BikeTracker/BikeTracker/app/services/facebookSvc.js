(function() {
        'use strict';

        var app = angular.module('Facebook', []);

        var serviceId = 'facebookSvc';

        app.factory(serviceId, ['$rootScope', 'bikeTrackerServer', facebookSvc]);

        function facebookSvc($rootScope, bikeTrackerServer) {
            // Define the functions and properties to reveal.
            var service = {
                getLoginStatus: getLoginStatus,
                login: login,
                logout: logout,
                unsubscribe: unsubscribe,
                fbConnected: fbConnected,
                watchAuthenticationStatusChange: watchAuthenticationStatusChange
            };

            return service;

            function getLoginStatus(success) {
                FB.getLoginStatus(function(response) {
                    $rootScope.$broadcast("fb_statusChange", { 'status': response.status });
                }, true);
            };

            function login() {
                FB.getLoginStatus(function(response) {
                    switch (response.status) {
                    case 'connected':
                        $rootScope.$broadcast('fb_connected', { facebook_id: response.authResponse.userID, accessToken: response.authResponse.accessToken });
                        break;
                    case 'not_authorized':
                    case 'unknown':
                        FB.login(function(response) {
                            if (response.authResponse) {
                                $rootScope.$broadcast('fb_connected', {
                                    facebook_id: response.authResponse.userID,
                                    userNotAuthorized: true,
                                    accessToken: response.authResponse.accessToken
                                });
                            } else {
                                $rootScope.$broadcast('fb_login_failed');
                            }
                        }, { scope: 'email' });
                        break;
                    default:
                        FB.login(function(response) {
                            if (response.authResponse) {
                                $rootScope.$broadcast('fb_connected', { facebook_id: response.authResponse.userID, userNotAuthorized: true, accessToken: response.authResponse.accessToken });
                                $rootScope.$broadcast('fb_get_login_status');
                            } else {
                                $rootScope.$broadcast('fb_login_failed');
                            }
                        });
                        break;
                    }
                }, true);
            };

            function logout() {
                FB.logout(function(response) {
                    if (response) {
                        $rootScope.$broadcast('fb_logout_succeded');
                    } else {
                        $rootScope.$broadcast('fb_logout_failed');
                    }
                });
            };

            function unsubscribe() {
                FB.api("/me/permissions", "DELETE", function(response){
                    $rootScope.$broadcast('fb_get_login_status');
                });
            };

            function fbConnected(event, args) {

                var params = {};

                function getAuthorizationFromServer(accessToken) {
                    bikeTrackerServer.authorize.get({ token: accessToken }, function (user) {

                    });
                }

                if (args.userNotAuthorized === true) {
                    console.log("user is connected to facebook but has not authorized our app");
                }
                else {
                    console.log("user is connected to facebook and has authorized our app");
                    //the parameter needed in that case is just the users facebook id
                    getAuthorizationFromServer(args.accessToken);
                }
            };

            function watchAuthenticationStatusChange() {
                var self = this;
                FB.Event.subscribe('auth.authResponseChange', function (response) {

                    if (response.status === 'connected') {
                        console.log("user is connected to facebook status");
                    }
                    else {
                        $rootScope.$broadcast('fb_logout_succeded');
                    }

                });
            };
        };
    }
)();