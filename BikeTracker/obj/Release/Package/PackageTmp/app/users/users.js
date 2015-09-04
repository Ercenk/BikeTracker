(function () {
    'use strict';

    var controllerId = 'users';

    // TODO: replace app with your module name
    angular.module('app').controller(controllerId,
        ['$scope', 'common', 'bikeTrackerServer', 'azureTableService', users]);

    function users($scope, common, bikeTrackerServer, azureTableService) {
        var $q = common.$q;
        var getLogFn = common.logger.getLogFn;
        var log = getLogFn(controllerId);

        var vm = this;

        $scope.title = 'users';
        $scope.activate = activate;

        vm.users = [];
        vm.gridOptions = {
            data: 'vm.users',
            columnDefs: [
                { field: 'FirstName', displayName: "First Name", width: "auto" },
                { field: 'LastName', displayName: "Last Name", width: "auto" },
                { field: 'Email', displayName: "Email", width: "auto" },
            ],
            selectedItems: vm.selected,
            enableColumnResize: true,
            multiSelect: false,
            enableSorting: false
        };


        function getUsers() {
            var deferred = $q.defer();

            bikeTrackerServer.usersUri.get().$promise.then(function(url) {
                azureTableService.getAllData(url.value).get({}, function(data) {

                    if (data !== null && Object.prototype.toString.call(data.value) === '[object Array]') {

                        _(data.value).forEach(function(user) {

                            vm.users.push({
                                FirstName: user.FirstName,
                                LastName: user.LastName,
                                Email: user.Email
                            });
                        });

                    }

                    deferred.resolve();
                });
            }, function() {
                deferred.reject();
            });

            return deferred.propromise;
        }
        activate();

        function activate() {
            var promises = [getUsers()];
            common.activateController(promises, controllerId)
                .then(function () { log('Activated Users View'); });
        }
    }
})();
