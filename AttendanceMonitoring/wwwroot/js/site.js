var teacherID;
var secretaryID;
var studentID;
//Reusable Codes
function showSuccessToast(message) {
    //$('#successToast .toast-body').text(message);
    $('#toast-message').text(message);
    $('#successToast').toast('show');
}
function showUpdateSuccessToast(message) {
    $('#update-toast-message').text(message);
    $('#updateSuccessToast').toast('show');
}

function showDangerToast() {
    $('#dangerToast').toast('show');
}

function loadSpinner() {
    $('#spinnerWrapper').removeClass("d-none");
}

function loadPageBlur() {
    $('#blurred-overlay-page').css('display', 'block');
}

function loadBlurBackground() {
    $('#blurred-overlay').css('display', 'block');
}

function hideBlurBackground() {
    $('#blurred-overlay').css('display', 'none');
}
//BEst approach Separate Function with Parameters (soc)"Separation of Concerns"
//yung function, dapat tumatanggap siya ng parameters
function loadViewModal(url, teacherID, modal) {
    $.ajax({
        url: url + teacherID,
        type: 'GET',
        success: function (html) {
            modal.find('#ViewModalBody').html(html);
        },
        error: function (xhr, status, error) {
            console.error('Error loading add teacher modal:', error);
            modal.find('#AddModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
        }
    });
}

$(document).ready(function () {

    //Event delegation for file input change
    $(document).on('change', 'input[type="file"].custom-file-input', function () {
        var input = this;
        //Update the custom file label with selected filename
        var fileName = input.files[0] ? input.files[0].name : 'Choose file'; //ternary operator(shorthand for if else)
        var label = $(input).next('.custom-file-label');
        label.text(fileName);

        //Display image when files is selected
        if (input.files && input.files[0]) { //0 Indicates always for true
            var reader = new FileReader();
            reader.onload = function (e) {
                //Find the image in the same modal/container
                $(input).closest('.modal-content, .container').find('#imagePreview').attr('src', e.target.result);
            }
            reader.readAsDataURL(input.files[0]);
        }
    });

    ///////////////////////////////////////////////////////////////////////////////

    //Triggers Add user modal
    $('#AddModalForm').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        modal.find('#AddModalFormLabel').text(title);
        loadBlurBackground();
        $.ajax({
            url: url,
            type: 'GET',
            success: function (html) {
                modal.find('#AddModalFormBody').html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error loading add teacher modal:', error);
                modal.find('#AddModalFormBody').html('<p class="text-center">Failed to load modal body</p>');
            }
        });
    });

    $('#AddModalForm').on('hide.bs.modal', function () {
        hideBlurBackground();
    });

    ///////////////////////////////////////////////////////////////////////////////

    //Triggers Edit Modal
    $('#EditModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);

        teacherID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);
        modal.find('#EditModalLabel').text(title);

        loadBlurBackground();

        if (!teacherID) {
            alert('Id does not found');
            return;
        }

        $.ajax({
            url: url + teacherID,
            type: 'GET',
            success: function (html) {
                modal.find('#EditModalBody').html(html);
            },
            error: function (xhr, status, error) {
                console.error('Error loading edit teacher modal:', error);
                modal.find('#EditModalBody').html('<p class="text-center">Failed to Load modal body</p>');
            }
        });
    });

    $('#EditModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    }); 

    ///////////////////////////////////////////////////////////////////////////////

    //Trigger View Modal
    $('#ViewModal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        teacherID = button.data('id');

        var url = button.data('url');
        var title = button.data('title');

        var modal = $(this);

        modal.find('#ViewModalLabel').text(title);

        loadBlurBackground();
        // Ipasa 'yung local variables sa function
        loadViewModal(url, teacherID, modal);
    });

    $('#ViewModal').on('hide.bs.modal', function () {
        hideBlurBackground();
    });

    ///////////////////////////////////////////////////////////////////////////////

    //Triggers ModalDelete
    $('#ModalDelete').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        teacherID = button.data('id');

        loadBlurBackground();
    });

    $('#ModalDelete').on('hide.bs.modal', function (event) {
        hideBlurBackground();
    });
});