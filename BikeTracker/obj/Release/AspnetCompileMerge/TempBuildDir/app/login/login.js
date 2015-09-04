(function () {
    'use strict';

    var controllerId = 'loginCtrl';

    angular.module('app').controller(controllerId, ['$rootScope', '$scope', 'facebookSvc', 'bikeTrackerServer', 'common', loginCtrl]);

    function loginCtrl($rootScope, $scope, facebookSvc, bikeTrackerServer, common) {
        var $q = common.$q;

        var vm = this;
        var logSuccess = common.logger.getLogFn(controllerId, 'success');

        vm.getLoginStatus = facebookSvc.getLoginStatus;
        vm.login = login;
        vm.logout = logout;
        vm.unsubscribe = facebookSvc.unsubscribe;
        vm.isAuthenticated = false;
        vm.isAdmin = false;

        activate();

        function login() {
            facebookSvc.login().then(function(status) {
                bikeTrackerServer.authorize.get({ token: status.accessToken }, function (user) {
                    vm.isAuthenticated = true;
                    console.log("authenticated");
                    if (user.role === "Admin") {
                        vm.isAdmin = true;
                        console.log("admin");
                        $rootScope.isAdmin = true;
                    } else {
                        vm.isAdmin = false;
                        $rootScope.isAdmin = false;
                    }
                });
            }, function() {
                vm.isAdmin = false;
                vm.isAuthenticated = false;
            });
        }

        function logout() {
            facebookSvc.logout(
                function() {
                    vm.isAuthenticated = false;
                    vm.isAdmin = false;
                    console.log("logout");
                    $scope.$apply();
                },
                function() {
                    console.log("cannot log out from Facebook!");
                });
        }

        function getLoginStatus() {
            var deferred = $q.defer();

            var promise = facebookSvc.getLoginStatus();
            promise.then(function (accessToken) {
                bikeTrackerServer.authorize.get({ token: accessToken }, function(user) {
                    vm.isAuthenticated = true;
                    if (user.role === "Admin") {
                        vm.isAdmin = true;
                        $rootScope.isAdmin = true;
                    } else {
                        vm.isAdmin = false;
                        $rootScope.isAdmin = false;
                    }
                });
                deferred.resolve();
            });

            return deferred.promise;
        }

        function activate() {
            logSuccess('Login loaded!', null, true);
            common.activateController([getLoginStatus()], controllerId);
        }
    }
})();