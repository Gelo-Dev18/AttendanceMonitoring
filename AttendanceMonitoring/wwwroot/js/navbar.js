$(document).ready(function () {
    $('#togglePassword').click(function (e) {
        e.preventDefault();

        var passwordInput = $('input[asp-for="NewPassword"]');
        var icon = $(this).find('span');

        if (passwordInput.attr('type') === 'password') {
            passwordInput.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            passwordInput.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    //$('#togglePassword').click(function () {
    //    const passwordField = $('input[asp-for="NewPassword"]');
    //    const passwordFieldType = passwordField.attr('type');
    //    if (passwordFieldType === 'password') {
    //        passwordField.attr('type', 'text');
    //        $(this).find('.fas').removeClass('fa-eye').addClass('fa-eye-slash');
    //    } else {
    //        passwordField.attr('type', 'password');
    //        $(this).find('.fas').removeClass('fa-eye-slash').addClass('fa-eye');
    //    }
    //});
});