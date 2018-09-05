import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { Dropdown, DropdownItem, DropdownMenu, DropdownToggle, NavLink } from 'reactstrap';

const propTypes = {
  accnt: PropTypes.bool,
  plchr: PropTypes.bool
};
const defaultProps = {
  accnt: false,
  plchr: false
};

class DefaultHeaderDropdown extends Component {
  constructor(props) {
    super(props);

    this.toggle = this.toggle.bind(this);
    this.state = {
      dropdownOpen: false
    };
  }

  toggle() {
    this.setState({
      dropdownOpen: !this.state.dropdownOpen
    });
  }

  logout() {
    sessionStorage.removeItem('token');
    window.location.replace("/login");
  }


  dropAccnt() {
    return (
      <Dropdown nav isOpen={this.state.dropdownOpen} toggle={this.toggle}>
        <DropdownToggle nav>
          <img src={"data:image/jpeg;base64," + sessionStorage.getItem('profileimage')} className="img-avatar" id="profileImageHeader"/>
        </DropdownToggle>
        <DropdownMenu right>
          <DropdownItem header tag="div" className="text-center" id="profileUserFullName"><strong>{sessionStorage.getItem('userfullname')}</strong></DropdownItem>
          <DropdownItem><NavLink href="/userprofile"><i className="fa fa-user" /> Profile</NavLink></DropdownItem>
          <DropdownItem divider />
          <div onClick={this.logout}><DropdownItem><i className="fa fa-lock" /> Logout</DropdownItem></div>
        </DropdownMenu>
      </Dropdown>
    );
  }

  dropPlaceHolder() {
    return (
      <Dropdown nav className="d-sm-down-none">
        <DropdownToggle nav />
      </Dropdown>
    );
  }

 
  render() {
    const { accnt, plchr } = this.props;
    return (
      accnt ? this.dropAccnt() :
        plchr ? this.dropPlaceHolder() : null
    );
  }
}

DefaultHeaderDropdown.propTypes = propTypes;
DefaultHeaderDropdown.defaultProps = defaultProps;

export default DefaultHeaderDropdown;
