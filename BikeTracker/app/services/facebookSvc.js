(function() {
        'use strict';

        var app = angular.module('Facebook', []);

        var serviceId = 'facebookSvc';

        app.factory(serviceId, ['$rootScope', 'common', facebookSvc]);

        function facebookSvc($rootScope, common) {

            var $q = common.$q;

            // Define the functions and properties to reveal.
            var service = {
                getLoginStatus: getLoginStatus,
                login: login,
                logout: logout,
                unsubscribe: unsubscribe,
                watchAuthenticationStatusChange: watchAuthenticationStatusChange
            };

            return service;

            function getLoginStatus() {
                var deferred = $q.defer();
                FB.getLoginStatus(function (response) {
                    if (response.status === 'connected') {
                        getFirstName();
                        deferred.resolve(response.authResponse.accessToken);
                    } else {
                        deferred.reject("User not authorized the app.");
                    }
                    $rootScope.$broadcast("fb_statusChange", { 'status': response.status });
                }, true);
                return deferred.promise;
            };

            function getFirstName() {
                FB.api("/me?fields=first_name", function(response) {
                    $rootScope.$broadcast('fb_login_succeeded', response.first_name);
                });
            }

            function login() {
                var deferred = $q.defer();
                FB.getLoginStatus(function (response) {
                    switch (response.status) {
                        case 'connected':
                            deferred.resolve({ facebook_id: response.authResponse.userID, accessToken: response.authResponse.accessToken, userNotAuthorized: true });
                            getFirstName();
                        break;
                    case 'not_authorized':
                    case 'unknown':
                        FB.login(function(response) {
                            if (!response.authResponse) {
                                $rootScope.$broadcast('fb_login_failed');
                                deferred.reject("login failed on Facebook.");
                            } else {
                                var statusDetails = { facebook_id: response.authResponse.userID, accessToken: response.authResponse.accessToken, userNotAuthorized: true };
                                getFirstName();
                                deferred.resolve(statusDetails);
                            }
                        }, { scope: 'email' });
                        break;
                    default:
                        FB.login(function(response) {
                            if (response.authResponse) {
                                getLoginStatus().then(function(token) {
                                    getFirstName();
                                }, function() {
                                    $rootScope.$broadcast('fb_login_failed');
                                    deferred.reject("login failed on Facebook.");
                                });
                                
                            } else {
                                $rootScope.$broadcast('fb_login_failed');
                                deferred.reject("login failed on Facebook.");
                            }
                        });
                        break;
                    }
                }, true);

                return deferred.promise;
            };

            function logout(success, fail) {
                FB.logout(function(response) {
                    if (response) {
                        success();
                    } else {
                        fail();
                    }
                });
            };

            function unsubscribe() {
                FB.api("/me/permissions", "DELETE", function(response){
                    getLoginStatus();
                });
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